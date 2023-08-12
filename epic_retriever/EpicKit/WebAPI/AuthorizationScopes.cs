using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using System.Runtime.Serialization;

namespace EpicKit.WebAPI
{
    [JsonConverter(typeof(StringEnumConverter))]
    public enum AuthorizationScopes
    {
        [EnumMember(Value = "basic_profile")]
        BasicProfile,
        [EnumMember(Value = "openid")]
        OpenId,
        [EnumMember(Value = "friends_list")]
        FriendsList,
        [EnumMember(Value = "presence")]
        Presence,
        [EnumMember(Value = "offline_access")]
        OfflineAccess,
        [EnumMember(Value = "friends_management")]
        FriendsManagement,
        [EnumMember(Value = "library")]
        Library,
        [EnumMember(Value = "country")]
        Country,
        [EnumMember(Value = "relevant_cosmetics")]
        RelevantCosmetics
    }

    public static class AuthorizationScopesExtensions
    {
        public static string ToApiString(this AuthorizationScopes scope)
        {
            try
            {
                var enumType = typeof(AuthorizationScopes);
                var memberInfos = enumType.GetMember(scope.ToString());
                var enumValueMemberInfo = memberInfos.FirstOrDefault(m => m.DeclaringType == enumType);
                var valueAttributes = enumValueMemberInfo.GetCustomAttributes(typeof(EnumMemberAttribute), false);
                return ((EnumMemberAttribute)valueAttributes[0]).Value;
            }
            catch
            {
                return scope.ToString();
            }
        }

        public static string JoinWithValue(this AuthorizationScopes[] scopes, string separator)
        {
            return string.Join(separator, scopes.Select(scope => scope.ToApiString()));
        }
    }
}
