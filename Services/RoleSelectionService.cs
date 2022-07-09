using System.Xml;
using static Ja3farBot.Util.FileDatatypes;

namespace Ja3farBot.Services
{
    public class RoleSelectionService
    {
        private readonly ConfigService _configService;
        public RoleSelectionService(ConfigService configService)
        {
            _configService = configService;
        }

        public RoleSetting GetRoleSetting(string Role)
        {
            string location = _configService.GetFilePath("roles.xml");
            FileStream stream = new(location, FileMode.Open, FileAccess.Read);
            XmlDocument xmlDoc = new();
            xmlDoc.Load(stream);
            stream.Dispose();

            RoleSetting roleSetting = new();

            foreach (XmlNode type in xmlDoc.DocumentElement) foreach (XmlNode role in type) if (role.Name == Role)
                    {
                        roleSetting.ID = ulong.Parse(type.Attributes.GetNamedItem("id").Value);
                        roleSetting.Group = type.Name;
                        foreach (XmlNode info in role) switch (info.Name)
                            {
                                case "roleid":
                                    roleSetting.RoleID = ulong.Parse(info.InnerText);
                                    break;
                                case "emote":
                                    roleSetting.Emote = info.InnerText;
                                    break;
                                case "emoji":
                                    roleSetting.Emoji = info.InnerText;
                                    break;
                            }
                    }

            return roleSetting;
        }
        public RoleSetting GetRoleSetting(ulong RoleId)
        {
            string location = _configService.GetFilePath("roles.xml");
            FileStream stream = new(location, FileMode.Open, FileAccess.Read);
            XmlDocument xmlDoc = new();
            xmlDoc.Load(stream);
            stream.Dispose();

            foreach (XmlNode type in xmlDoc.DocumentElement) foreach (XmlNode role in type) foreach (XmlNode info in role) if (info.InnerText == RoleId.ToString()) return GetRoleSetting(role.Name);
            return new();
        }

        public ulong GetMessageID(string Category)
        {
            string location = _configService.GetFilePath("roles.xml");
            FileStream stream = new(location, FileMode.Open, FileAccess.Read);
            XmlDocument xmlDoc = new();
            xmlDoc.Load(stream);
            stream.Dispose();

            foreach (XmlNode type in xmlDoc.DocumentElement) if (type.Name == Category) return ulong.Parse(type.Attributes.GetNamedItem("id").Value);
            return 0;
        }
    }
}
