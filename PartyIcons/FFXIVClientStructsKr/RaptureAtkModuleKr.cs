using System.Runtime.InteropServices;
using FFXIVClientStructs.FFXIV.Client.Game.Object;
using FFXIVClientStructs.FFXIV.Client.System.String;
using FFXIVClientStructs.FFXIV.Component.GUI;

namespace PartyIcons.FFXIVClientStructsKr
{
    [StructLayout(LayoutKind.Explicit, Size = 164256)]
    public struct RaptureAtkModuleKr
    {
        [StructLayout(LayoutKind.Explicit, Size = 584)]
        public struct NamePlateInfo
        {
            [FieldOffset(0)]
            public GameObjectID ObjectID;

            [FieldOffset(48)]
            public Utf8String Name;

            [FieldOffset(152)]
            public Utf8String FcName;

            [FieldOffset(256)]
            public Utf8String Title;

            [FieldOffset(360)]
            public Utf8String DisplayTitle;

            [FieldOffset(464)]
            public Utf8String LevelText;
        }

        [FieldOffset(0)]
        public AtkModule AtkModule;

        [FieldOffset(110008)] // 107880 -> 110008
        public NamePlateInfo NamePlateInfoArray;
    }
}
