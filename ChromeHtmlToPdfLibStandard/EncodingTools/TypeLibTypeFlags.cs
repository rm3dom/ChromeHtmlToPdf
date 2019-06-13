namespace System.Runtime.InteropServices
{
  /// <summary>Describes the original settings of the <see cref="T:System.Runtime.InteropServices.TYPEFLAGS" /> in the COM type library from which the type was imported.</summary>
  [Flags]
  [ComVisible(true)]
  [Serializable]
  public enum TypeLibTypeFlags
  {
    /// <summary>A type description that describes an <see langword="Application" /> object.</summary>
    FAppObject = 1,
    /// <summary>Instances of the type can be created by <see langword="ITypeInfo::CreateInstance" />.</summary>
    FCanCreate = 2,
    /// <summary>The type is licensed.</summary>
    FLicensed = 4,
    /// <summary>The type is predefined. The client application should automatically create a single instance of the object that has this attribute. The name of the variable that points to the object is the same as the class name of the object.</summary>
    FPreDeclId = 8,
    /// <summary>The type should not be displayed to browsers.</summary>
    FHidden = 16, // 0x00000010
    /// <summary>The type is a control from which other types will be derived, and should not be displayed to users.</summary>
    FControl = 32, // 0x00000020
    /// <summary>The interface supplies both <see langword="IDispatch" /> and V-table binding.</summary>
    FDual = 64, // 0x00000040
    /// <summary>The interface cannot add members at run time.</summary>
    FNonExtensible = 128, // 0x00000080
    /// <summary>The types used in the interface are fully compatible with Automation, including vtable binding support.</summary>
    FOleAutomation = 256, // 0x00000100
    /// <summary>This flag is intended for system-level types or types that type browsers should not display.</summary>
    FRestricted = 512, // 0x00000200
    /// <summary>The class supports aggregation.</summary>
    FAggregatable = 1024, // 0x00000400
    /// <summary>The object supports <see langword="IConnectionPointWithDefault" />, and has default behaviors.</summary>
    FReplaceable = 2048, // 0x00000800
    /// <summary>Indicates that the interface derives from <see langword="IDispatch" />, either directly or indirectly.</summary>
    FDispatchable = 4096, // 0x00001000
    /// <summary>Indicates base interfaces should be checked for name resolution before checking child interfaces. This is the reverse of the default behavior.</summary>
    FReverseBind = 8192, // 0x00002000
  }
}