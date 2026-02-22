using System;

namespace StudioElevenLib.Level5.Binary.Logic
{
    [AttributeUsage(AttributeTargets.Property)]
    public class CfgBinIgnoreAttribute : Attribute
    {

    }

    [AttributeUsage(AttributeTargets.Property)]
    public class CfgBinNatOrderAttribute : Attribute
    {
        public int Order { get; }
        public CfgBinNatOrderAttribute(int order) => Order = order;
    }
}
