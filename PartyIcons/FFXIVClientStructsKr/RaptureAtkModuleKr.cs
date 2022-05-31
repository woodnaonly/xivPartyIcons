using System.Runtime.InteropServices;
using FFXIVClientStructs.FFXIV.Client.Game.Object;
using FFXIVClientStructs.FFXIV.Client.System.String;
using FFXIVClientStructs.FFXIV.Component.GUI;

namespace PartyIcons.FFXIVClientStructsKr
{
    [StructLayout(LayoutKind.Explicit, Size = 162112)]
    public struct RaptureAtkModuleKr
    {
        [StructLayout(LayoutKind.Explicit, Size = 584)]
        public struct NamePlateInfo
        {
            [FieldOffset(0)]
            public GameObjectID ObjectID;

            [FieldOffset(48 + 34)]
            public Utf8String Name;

            [FieldOffset(152 + 34)]
            public Utf8String FcName;

            [FieldOffset(256 + 34)]
            public Utf8String Title;

            [FieldOffset(360 + 34)]
            public Utf8String DisplayTitle;
        }

        [FieldOffset(0)]
        public AtkModule AtkModule;

        [FieldOffset(107864)] // 107880 -> 107864
        public NamePlateInfo NamePlateInfoArray;
    }
}
