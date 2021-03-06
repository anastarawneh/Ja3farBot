using System.Collections.Generic;

namespace rJordanBot.Resources.Datatypes
{
    public class ConfigFile
    {
        public string token { get; set; }
        public ulong owner { get; set; }
        public List<ulong> reportbanned { get; set; }
        public int starboardmin { get; set; }
        public bool modappsactive { get; set; }
        public bool eventsactive { get; set; }
        public List<string> invitewhitelist { get; set; }
        public ulong verifyid { get; set; }
        public List<string> bannedwords { get; set; }
        public bool music { get; set; }
        public Announcement announcement { get; set; }
        public string mysql_server { get; set; }
        public string mysql_username { get; set; }
        public string mysql_password { get; set; }
        public string mysql_dbname { get; set; }
        public List<ulong> ignoredverfmsgs { get; set; }

        public class Announcement
        {
            public string title { get; set; }
            public string desc { get; set; }
            public Field[] fields { get; set; }

            public class Field
            {
                public string title { get; set; }
                public string content { get; set; }
            }
        }
    }

    public class RoleSetting
    {
        public ulong id { get; set; }
        public ulong roleid { get; set; }
        public string emote { get; set; }
        public string group { get; set; }
        public string emoji { get; set; }
    }
}
