
namespace LearningApi.Models
{
    public class CheckCreditnials
    {
        public readonly Neo4jService _neo4JService;

        public CheckCreditnials(Neo4jService neo4JService)
        {
            _neo4JService = neo4JService;
        }

        public async Task<Dictionary<string, object>?> CheckForCredintials(Dictionary<string, object> parameters) //return the userdata if the creditnials are correct
        {
            var User = await _neo4JService.ReadQuery("MATCH (n:User) WHERE n.username = $username RETURN n", parameters);
            if (User.Count == 0)
            {
                return null;
            }

            var UserData = (Dictionary<string, object>)User[0];
            UserData.TryGetValue("password", out var TruepasswordObj);
            string Truepassword = (string)TruepasswordObj!;
            var password = parameters["password"].ToString();
            return BCrypt.Net.BCrypt.Verify(password, Truepassword) ? UserData : null;
        }

        public async Task<bool> CheckForQuery(string query, Dictionary<string, object> parameters) //return true if the query returned smthing
        {
            var User = await _neo4JService.ReadQuery(query, parameters);
            return User.Count == 0 ? false : true;

        }

        //public async Task<bool> CheckForEmail(Dictionary<string, object> parameters)// return true if the email was found
        //{
        //    var User = await _neo4JService.ReadQuery("MATCH (n:User) WHERE n.email = $email RETURN n", parameters);
        //    return User.Count == 0 ? false : true;
        //}



    }
}
