#nullable enable

// aliases for name in Rust
global using u8 = System.Byte;
global using i8 = System.SByte;
global using u16 = System.UInt16;
global using i16 = System.Int16;
global using u32 = System.UInt32;
global using i32 = System.Int32;
global using u64 = System.UInt64;
global using i64 = System.Int64;
global using u128 = System.UInt128;
global using i128 = System.Int128;
global using f32 = System.Single;
global using f64 = System.Double;
#pragma warning disable CS8981 // lowercase ASCII-only aliases may conflict with reserved names in the future.
global using usize = System.UIntPtr;
#pragma warning restore CS8981 // lowercase ASCII-only aliases may conflict with reserved names in the future.

global using CE = Elffy.NativeBind.CoreElffy;

global using EnumMapping;
