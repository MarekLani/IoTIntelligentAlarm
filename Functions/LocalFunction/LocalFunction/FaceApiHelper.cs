using Microsoft.Azure.WebJobs.Host;
using Microsoft.ProjectOxford.Face;
using Microsoft.ProjectOxford.Face.Contract;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

public class FaceServiceHelper
{

    public Dictionary<string, FaceListInfo> faceLists;

    public static TraceWriter log;

    public class FaceListInfo
    {
        public string FaceListId { get; set; }
        public DateTime LastMatchTimestamp { get; set; }
        public bool IsFull { get; set; }
    }

    public async Task InitializeFaceLists()
    {
        faceLists = new Dictionary<string, FaceListInfo>();
        try
        {
            IEnumerable<FaceListMetadata> metadata = await GetFaceListsAsync();
            foreach (var item in metadata)
            {
                faceLists.Add(item.FaceListId, new FaceListInfo { FaceListId = item.FaceListId, LastMatchTimestamp = DateTime.Now });
            }
        }
        catch (Exception e)
        {
            log.Info("Face API GetFaceListsAsync error: " + e.Message);
        }
    }

    public async Task<Tuple<SimilarPersistedFace, string>> FindBestMatch(Guid faceId)
    {

        if (faceLists == null)
        {
            await InitializeFaceLists();
        }

        Tuple<SimilarPersistedFace, string> bestMatch = null;

        foreach (var faceListId in faceLists.Keys)
        {
            try
            {
                SimilarPersistedFace similarFace = null;
                try
                {
                    similarFace = (await FindSimilarAsync(faceId, faceListId))?.FirstOrDefault();
                }
                catch (Exception e)
                {
                    if (e.GetType() == typeof(FaceAPIException))
                        log.Info(((FaceAPIException)e).ErrorMessage);
                    else
                        log.Info("Face API Find Similar error: " + e.Message);
                }
                if (similarFace != null)
                {
                    if (bestMatch != null)
                    {
                        // We already found a match for this face in another list. Replace the previous one if the new confidence is higher.
                        if (bestMatch.Item1.Confidence < similarFace.Confidence)
                        {
                            bestMatch = new Tuple<SimilarPersistedFace, string>(similarFace, faceListId);
                        }
                    }
                    else
                    {
                        bestMatch = new Tuple<SimilarPersistedFace, string>(similarFace, faceListId);
                    }
                }
            }
            catch (Exception ex)
            {
                // Catch errors with individual face lists so we can continue looping through all lists. Maybe an answer will come from
                // another one.
                log.Info("Face API FindSimilarAsync error " + ex.Message.ToString() + ex.StackTrace);
            }
        }
        return bestMatch;
    }

    public async Task<SimilarPersistedFace> AddPersonToListAndCreateListIfNeeded(string imageUri, FaceRectangle faceRectangle)
    {

        // If we are here we didnt' find a match, so let's add the face to the first FaceList that we can add it to. We
        // might create a new list if none exist, and if all lists are full we will delete the oldest face list (based on when we  
        // last matched anything on it) so that we can add the new one.
        if (faceLists == null)
        {
            await InitializeFaceLists();
        }

        if (!faceLists.Any())
        {
            // We don't have any FaceLists yet. Create one
            string newFaceListId = Guid.NewGuid().ToString();
            await CreateFaceListAsync(newFaceListId, "ManagedFaceList");

            faceLists.Add(newFaceListId, new FaceListInfo { FaceListId = newFaceListId, LastMatchTimestamp = DateTime.Now });
        }

        AddPersistedFaceResult addResult = null;
        bool failedToAddToNonFullList = false;
        foreach (var faceList in faceLists)
        {
            if (faceList.Value.IsFull)
            {
                continue;
            }

            try
            {
                addResult = await AddFaceToFaceListAsync(faceList.Key, imageUri, faceRectangle);
                break;
            }
            catch (Exception ex)
            {
                if (ex is FaceAPIException && ((FaceAPIException)ex).ErrorCode == "403")
                {
                    // FaceList is full. Continue so we can try again with the next FaceList
                    faceList.Value.IsFull = true;
                    continue;
                }
                else
                {
                    failedToAddToNonFullList = true;
                    break;
                }
            }
        }

        if (addResult == null && !failedToAddToNonFullList)
        {
            // We were not able to add the face to an existing list because they were all full. 

            // If possible, let's create a new list now and add the new face to it. If we can't (e.g. we already maxed out on list count), 
            // let's delete an old list, create a new one and add the new face to it.

            if (faceLists.Count == 64)
            {
                // delete oldest face list
                var oldestFaceList = faceLists.OrderBy(fl => fl.Value.LastMatchTimestamp).FirstOrDefault();
                faceLists.Remove(oldestFaceList.Key);
                await DeleteFaceListAsync(oldestFaceList.Key);
            }

            // create new list
            string newFaceListId = Guid.NewGuid().ToString();
            await CreateFaceListAsync(newFaceListId, "ManagedFaceList");
            faceLists.Add(newFaceListId, new FaceListInfo { FaceListId = newFaceListId, LastMatchTimestamp = DateTime.Now });

            // Add face to new list
            addResult = await AddFaceToFaceListAsync(newFaceListId, imageUri, faceRectangle);
        }

        if (addResult != null)
        {
            return new SimilarPersistedFace() { Confidence = 1, PersistedFaceId = addResult.PersistedFaceId };
        }
        else
        {
            log.Info("Returning null, added face not success");
            return null;
        }

    }

    public static int RetryCountOnQuotaLimitError = 7;
    public static int RetryDelayOnQuotaLimitError = 700;

    private FaceServiceClient faceClient { get; set; }

    public Action Throttled;

    private static string apiKey;
    public static string ApiKey
    {
        get { return apiKey; }
        set
        {
            apiKey = value;
        }
    }

    public FaceServiceHelper()
    {
        InitializeFaceServiceClient();

    }

    private void InitializeFaceServiceClient()
    {
        faceClient = new FaceServiceClient(apiKey);
    }


    private async Task<TResponse> RunTaskWithAutoRetryOnQuotaLimitExceededError<TResponse>(Func<Task<TResponse>> action)
    {
        int retriesLeft = FaceServiceHelper.RetryCountOnQuotaLimitError;
        int delay = FaceServiceHelper.RetryDelayOnQuotaLimitError;

        TResponse response = default(TResponse);

        while (true)
        {
            try
            {
                response = await action();
                break;
            }
            catch (FaceAPIException exception) when (exception.HttpStatus == (System.Net.HttpStatusCode)429 && retriesLeft > 0)
            {
                if (retriesLeft == 1 && Throttled != null)
                {
                    Throttled();
                }

                await Task.Delay(delay);
                retriesLeft--;
                delay *= 2;
                continue;
            }
        }

        return response;
    }

    private async Task RunTaskWithAutoRetryOnQuotaLimitExceededError(Func<Task> action)
    {
        await RunTaskWithAutoRetryOnQuotaLimitExceededError<object>(async () => { await action(); return null; });
    }


    public async Task<Face[]> DetectAsync(string url, bool returnFaceId = true, bool returnFaceLandmarks = false, IEnumerable<FaceAttributeType> returnFaceAttributes = null)
    {
        return await RunTaskWithAutoRetryOnQuotaLimitExceededError<Face[]>(() => faceClient.DetectAsync(url, returnFaceId, returnFaceLandmarks, returnFaceAttributes));
    }

    public async Task<AddPersistedFaceResult> AddFaceToFaceListAsync(string faceListId, string imageUri, FaceRectangle targetFace)
    {
        return (await RunTaskWithAutoRetryOnQuotaLimitExceededError<AddPersistedFaceResult>(() => faceClient.AddFaceToFaceListAsync(faceListId, imageUri, null, targetFace)));
    }

    public async Task<IEnumerable<FaceListMetadata>> GetFaceListsAsync(string userDataFilter = null)
    {
        return (await RunTaskWithAutoRetryOnQuotaLimitExceededError<FaceListMetadata[]>(() => faceClient.ListFaceListsAsync())).Where(list => string.IsNullOrEmpty(userDataFilter) || string.Equals(list.UserData, userDataFilter));
    }

    public async Task<SimilarPersistedFace[]> FindSimilarAsync(Guid faceId, string faceListId, int maxNumOfCandidatesReturned = 1)
    {
        return (await RunTaskWithAutoRetryOnQuotaLimitExceededError<SimilarPersistedFace[]>(() => faceClient.FindSimilarAsync(faceId, faceListId, maxNumOfCandidatesReturned)));
    }

    public async Task CreateFaceListAsync(string faceListId, string name, string userData)
    {
        await RunTaskWithAutoRetryOnQuotaLimitExceededError(() => faceClient.CreateFaceListAsync(faceListId, name, userData));
    }
    public async Task CreateFaceListAsync(string faceListId, string name)
    {
        await RunTaskWithAutoRetryOnQuotaLimitExceededError(() => faceClient.CreateFaceListAsync(faceListId, name, ""));
    }

    public async void DeleteAllFaceLists()
    {
        if (faceLists == null)
        {
            await InitializeFaceLists();
        }

        foreach (var faceListId in faceLists.Keys)
        {
            await DeleteFaceListAsync(faceListId);
        }

        faceLists = null;
    }

    public async Task DeleteFaceListAsync(string faceListId)
    {
        await RunTaskWithAutoRetryOnQuotaLimitExceededError(() => faceClient.DeleteFaceListAsync(faceListId));
    }

    public async Task DeleteFaceFromFaceListAsync(string faceListId, Guid peristedFaceId)
    {
        await RunTaskWithAutoRetryOnQuotaLimitExceededError(() => faceClient.DeleteFaceFromFaceListAsync(faceListId, peristedFaceId));
    }

    public async Task<PersonFace> GetPersonFaceAsync(string personGroupId, Guid personId, Guid face)
    {
        return await RunTaskWithAutoRetryOnQuotaLimitExceededError<PersonFace>(() => faceClient.GetPersonFaceAsync(personGroupId, personId, face));
    }

    public async Task<IdentifyResult[]> IdentifyAsync(string personGroupId, Guid[] detectedFaceIds)
    {
        return await RunTaskWithAutoRetryOnQuotaLimitExceededError<IdentifyResult[]>(() => faceClient.IdentifyAsync(personGroupId, detectedFaceIds));
    }

    public async Task<Person> GetPersonAsync(string personGroupId, Guid personId)
    {
        return await RunTaskWithAutoRetryOnQuotaLimitExceededError<Person>(() => faceClient.GetPersonAsync(personGroupId, personId));
    }

}

// public class EmotionData
// {
//     public string EmotionName { get; set; }
//     public float EmotionScore { get; set; }
// }

// public static class EmotionServiceHelper
// {
//     public static int RetryCountOnQuotaLimitError = 6;
//     public static int RetryDelayOnQuotaLimitError = 500;

//     public static TraceWriter log;

//     private static EmotionServiceClient emotionClient { get; set; }

//     static EmotionServiceHelper()
//     {
//         InitializeEmotionService();
//     }

//     public static Action Throttled;

//     private static string apiKey;
//     public static string ApiKey
//     {
//         get { return apiKey; }
//         set
//         {
//             var changed = apiKey != value;
//             apiKey = value;
//             if (changed)
//             {
//                 InitializeEmotionService();
//             }
//         }
//     }

//     private static void InitializeEmotionService()
//     {
//         emotionClient = new EmotionServiceClient(apiKey);
//     }

//     private static async Task<TResponse> RunTaskWithAutoRetryOnQuotaLimitExceededError<TResponse>(Func<Task<TResponse>> action)
//     {
//         int retriesLeft = FaceServiceHelper.RetryCountOnQuotaLimitError;
//         int delay = FaceServiceHelper.RetryDelayOnQuotaLimitError;

//         TResponse response = default(TResponse);

//         while (true)
//         {
//             try
//             {
//                 response = await action();
//                 break;
//             }
//             catch (ClientException exception) when (exception.HttpStatus == (System.Net.HttpStatusCode)429 && retriesLeft > 0)
//             {
//                 log.Info("Emotion API throttling error " + exception.Message.ToString());

//                 if (retriesLeft == 1 && Throttled != null)
//                 {
//                     Throttled();
//                 }

//                 await Task.Delay(delay);
//                 retriesLeft--;
//                 delay *= 2;
//                 continue;
//             }
//         }

//         return response;
//     }

//     private static async Task RunTaskWithAutoRetryOnQuotaLimitExceededError(Func<Task> action)
//     {
//         await RunTaskWithAutoRetryOnQuotaLimitExceededError<object>(async () => { await action(); return null; });
//     }

//     public static async Task<Emotion[]> RecognizeWithFaceRectanglesAsync(string imageUrl, Rectangle[] faceRectangles)
//     {
//         return await RunTaskWithAutoRetryOnQuotaLimitExceededError<Emotion[]>(() => emotionClient.RecognizeAsync(imageUrl, faceRectangles));
//     }

//     public static IEnumerable<EmotionData> ScoresToEmotionData(Scores scores)
//     {
//         List<EmotionData> result = new List<EmotionData>();
//         result.Add(new EmotionData { EmotionName = "Anger", EmotionScore = scores.Anger });
//         result.Add(new EmotionData { EmotionName = "Contempt", EmotionScore = scores.Contempt });
//         result.Add(new EmotionData { EmotionName = "Disgust", EmotionScore = scores.Disgust });
//         result.Add(new EmotionData { EmotionName = "Fear", EmotionScore = scores.Fear });
//         result.Add(new EmotionData { EmotionName = "Happiness", EmotionScore = scores.Happiness });
//         result.Add(new EmotionData { EmotionName = "Neutral", EmotionScore = scores.Neutral });
//         result.Add(new EmotionData { EmotionName = "Sadness", EmotionScore = scores.Sadness });
//         result.Add(new EmotionData { EmotionName = "Surprise", EmotionScore = scores.Surprise });

//         return result;
//     }
// }
