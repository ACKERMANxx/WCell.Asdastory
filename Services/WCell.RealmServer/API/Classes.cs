using RestSharp;
using System;
using System.Collections.Generic;
using System.Net;

namespace WCell.RealmServer.API
{
    public static class ClientAPI
    {
        private static RestClient _Client { get; set; }
        public static RestClient Client
        {
            get
            {
                if (_Client != null)
                    return _Client;
                else
                {
                    _Client = new RestClient("https://api.asdastory.me/") { Timeout = 2000 };
                    _Client.AddDefaultParameter("auth", "GameServer-Yt9dAJX3Etvy5eAAfnKGwEP8hcqFbbabXqkwxKMwDWroGCScJFNTeAKyZR5L2gS2W8NpxZ5GJXTJKhdyw6pCMZMQnU7XD95qK68tNo6pkmJQvjaHEg8H8haFFBfKmE2u", ParameterType.GetOrPost);
                    return _Client;
                }
            }
        }
    }

    public static class Requests
    {
        public static bool GetMallPoints(string username, out int points)
        {
            points = 0;
            return true;

            var request = new RestRequest(Method.POST);
            request.AddParameter("op", "GetMallPoints", ParameterType.GetOrPost);
            request.AddParameter("username", username, ParameterType.GetOrPost);
            var response = API.ClientAPI.Client.Execute<AsdaStoryAPIMallPointsRespond>(request);
            Console.WriteLine("Username: " + username);
            Console.WriteLine(response.Data.error);
            if (response.ResponseStatus == ResponseStatus.Completed && response.StatusCode == HttpStatusCode.OK && response.Data.success)
            {
                points = response.Data.points;
                return true;
            }
            else
            {
                points = 0;
                return false;
            }
        }
        private static readonly Object obj = new Object();
        public static bool AddMallPoints(string username, int points, string reason)
        {
            return false;
            lock (obj)
            {
                var request = new RestRequest(Method.POST);
                request.Timeout = 40000;
                request.AddParameter("op", "AddMallPoints", ParameterType.GetOrPost);
                request.AddParameter("username", username, ParameterType.GetOrPost);
                request.AddParameter("reason", reason, ParameterType.GetOrPost);
                request.AddParameter("pointstoadd", points, ParameterType.GetOrPost);
                var response = API.ClientAPI.Client.Execute<AddMallPointsRespond>(request);
                if (response.ResponseStatus == ResponseStatus.Completed && response.StatusCode == HttpStatusCode.OK && response.Data.success)
                {
                    return true;
                }
                else
                {
                    return false;
                }
            }
        }
        private static readonly Object obj2 = new Object();
        public static bool SubMallPoints(string username, int points, string reason, out int newpoints)
        {
            newpoints = 0;
            return false;
            lock (obj2)
            {
                var request = new RestRequest(Method.POST);
                request.Timeout = 40000;
                request.AddParameter("op", "SubMallPoints", ParameterType.GetOrPost);
                request.AddParameter("username", username, ParameterType.GetOrPost);
                request.AddParameter("reason", reason, ParameterType.GetOrPost);
                request.AddParameter("pointstosub", points, ParameterType.GetOrPost);
                var response = API.ClientAPI.Client.Execute<AddMallPointsRespond>(request);
                if (response.ResponseStatus == ResponseStatus.Completed && response.StatusCode == HttpStatusCode.OK && response.Data.success)
                {
                    newpoints = response.Data.newpoints;
                    return true;
                }
                else
                {
                    if (response != null && response.Data != null && response.Data.newpoints != null)
                    {
                        newpoints = response.Data.newpoints;
                    }
                    else
                        newpoints = 0;
                    return false;
                }
            }
        }
        public static bool UpdateTitlesClassement(List<Rank> ranks)
        {
            return false;
            var request = new RestRequest(Method.POST);
            request.Timeout = 60000;
            request.AddParameter("op", "UpdateTitlesClassement", ParameterType.GetOrPost);
            request.AddParameter("data", JsonHelper.Serialize(ranks), ParameterType.GetOrPost);
            var response = API.ClientAPI.Client.Execute<AsdaStoryAPIRespond>(request);
            if (response.ResponseStatus == ResponseStatus.Completed && response.StatusCode == HttpStatusCode.OK && response.Data.success)
            {
                Console.WriteLine("Website Ranking Updated.");
                return true;
            }
            else
            {
                Console.WriteLine("ERROR: Update Tiltes in the website failed.");
                return false;
            }

        }
    }
    public class Rank
    {
        public string Name { get; set; }
        public int Class { get; set; }
    }
    public class AsdaStoryAPIDefaultRespond
    {
        public bool success { get; set; }
        public string error { get; set; }
    }

    public class AsdaStoryAPIRespond : AsdaStoryAPIDefaultRespond
    {

    }
    public class LoginAsdaStoryAPIRespond : AsdaStoryAPIDefaultRespond
    {
        public int CBTTester { get; set; }

    }
    public class AsdaStoryAPIMallPointsRespond : AsdaStoryAPIDefaultRespond
    {
        public int points { get; set; }
    }
    public class AddMallPointsRespond : AsdaStoryAPIDefaultRespond
    {
        public int oldpoints { get; set; }
        public int newpoints { get; set; }
    }

}
