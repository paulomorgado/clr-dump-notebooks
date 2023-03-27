public struct Address : IEquatable<Address>, IComparable<Address>
{
    private ulong value;

    public int CompareTo(Address other) => this.value.CompareTo(other.value);

    public bool Equals(Address other) => this.value == other.value;

    public override bool Equals(object? obj)
        => (obj is Address other && this.Equals(other))
            || (obj is ulong num && this.value.Equals(num));

    public override int GetHashCode() => this.value.GetHashCode();

    public override string ToString() => this.value.ToString("x16");

    public static implicit operator ulong(Address address) => address.value;

    public static implicit operator Address(ulong num) => new() { value = num };
}

public static bool IsOneOf(this ClrObject clrObject, params string[] types)
{
    for (var baseType = clrObject.Type; baseType is not null; baseType = baseType.BaseType)
    {
        if (types.Contains(baseType.Name, StringComparer.Ordinal))
        {
            return true;
        }
    }

    return false;
}

public static ulong GetWeakReferenceTargetAddress(this ClrObject clrObject)
{
    var handle = (ulong)unchecked((long)clrObject.ReadField<IntPtr>("m_handle"));
    var targetAddress = clrObject.Type.Heap.Runtime.DataTarget.DataReader.ReadPointer(handle);
    return targetAddress;
}

public static ClrObject GetWeakReferenceTarget(this ClrObject clrObject)
{
    return clrObject.Type.Heap.GetObject(clrObject.GetWeakReferenceTargetAddress());
}
