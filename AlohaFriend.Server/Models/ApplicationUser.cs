using Newtonsoft.Json;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Runtime.Serialization;

namespace AlohaFriend.Server.Models
{
    public class ApplicationUser
    {
        public int Id { get; set; }
        public string PhoneNumber { get; set; }
        public string PasswordHash { get; set; }
        [JsonIgnore]
        [IgnoreDataMember]
        public ICollection<ApplicationUserChatRoomJunction> ChatRooms { get; set; }
    }
}
