using SQLite;

namespace Disa.Framework
{
    internal class BubbleGroupSettings
    {
        [PrimaryKey, AutoIncrement]
        public int Id { get; set; }

        public string Guid { get; set; }

        public bool Mute { get; set; }

        public int NotificationLed { get; set; }

        public string VibrateOption { get; set; }

        public string VibrateOptionCustomPattern { get; set; }

        public string Ringtone { get; set; }

        public bool Unread { get; set; }

        public long LastUnreadSetTime { get; set; }

        public byte[] ParticipantNicknames { get; set; }

        [Ignore]
        public DisaParticipantNickname[] ParticipantNicknamesCached { get; set; }
        [Ignore]
        public bool ParticipantNicknamesCachedSet { get; set; }

        public byte[] ReadTimes { get; set; }

        [Ignore]
        public DisaReadTime[] ReadTimesCached { get; set; }
        [Ignore]
        public bool ReadTimesCachedSet { get; set; }
    }
}