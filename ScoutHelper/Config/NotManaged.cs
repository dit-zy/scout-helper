using System;

namespace ScoutHelper.Config;

[AttributeUsage(AttributeTargets.Field | AttributeTargets.Property, Inherited = false)]
public class NotManaged : Attribute;
