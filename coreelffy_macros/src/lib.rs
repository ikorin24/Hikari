use proc_macro::TokenStream;
use quote::ToTokens;
use syn::{self, Lit, NestedMeta};
use syn::{parse_macro_input, AttributeArgs};

#[proc_macro_attribute]
pub fn tagged_ref_union(args: TokenStream, input: TokenStream) -> TokenStream {
    let target = parse_macro_input!(input as syn::ItemStruct);
    let name = &target.ident.to_string();
    let union_types: Vec<String> = parse_macro_input!(args as AttributeArgs)
        .iter()
        .map(|arg| {
            if let NestedMeta::Lit(Lit::Str(s)) = arg {
                s.value()
            } else {
                panic!("all args must be literal str");
            }
        })
        .collect();
    let vis = target.vis.to_token_stream().to_string();

    let tag_name = format!("{name}Tag");
    let payload_name = format!("{name}Payload");
    let enum_name = format!("{name}Enum");

    let compat_enum = {
        let members = union_types
            .iter()
            .enumerate()
            .map(|(i, ty)| format!(r"Type{i}(&'a {ty})"))
            .collect::<Vec<_>>()
            .join(",\n");
        format!(
            r"{vis} enum {enum_name}<'a> {{
                    {members}
            }}"
        )
    };

    let to_enum_method_impl = {
        let cases = union_types
            .iter()
            .enumerate()
            .map(|(i, _)| {
                format!(
                    r"
            {tag_name}::Type{i} => {enum_name}::Type{i}(
                unsafe{{ self.payload.cast{i}() }}
            )"
                )
            })
            .collect::<Vec<_>>()
            .join(",\n");
        format!(
            r"#[inline]
            pub fn to_enum(&self) -> {enum_name} {{
                match self.tag {{
                    {cases}
                }}
            }}"
        )
    };

    let map_method_impl = {
        let arg_defs = union_types
            .iter()
            .enumerate()
            .map(|(i, ty)| format!(r"map{i}: impl FnOnce(&{ty}) -> T"))
            .collect::<Vec<_>>()
            .join(",");
        let match_cases = union_types
            .iter()
            .enumerate()
            .map(|(i, _)| {
                format!(r"{tag_name}::Type{i} => map{i}(unsafe {{ self.payload.cast{i}() }})")
            })
            .collect::<Vec<_>>()
            .join(",");
        format!(
            r"pub fn map<T>(&self, {arg_defs}) -> T {{
            match self.tag {{
                {match_cases}
            }}
        }}"
        )
    };

    let tags = union_types
        .iter()
        .enumerate()
        .map(|(i, ty)| {
            format!(
                r"/// payload is `&{ty}`
                Type{i} = {i},
                "
            )
        })
        .collect::<Vec<_>>()
        .join("\n");

    let payload_methods_impl = union_types
        .iter()
        .enumerate()
        .map(|(i, arg)| {
            format!(
                r"#[inline]
                pub unsafe fn cast{i}(&self) -> &'a {arg} {{
                    self.ptr.cast().as_ref()
            }}"
            )
        })
        .collect::<Vec<_>>()
        .join("\n");

    format!(
        r"
        #[repr(C)]
        {vis} struct {name}<'a> {{
            tag: {tag_name},
            payload: {payload_name}<'a>,
        }}

        impl<'a> {name}<'a> {{
            {map_method_impl}
            {to_enum_method_impl}
        }}

        #[repr(transparent)]
        {vis} struct {payload_name}<'a> {{
            ptr: std::ptr::NonNull<std::ffi::c_void>,
            __: std::marker::PhantomData<&'a std::ffi::c_void>,
        }}

        impl<'a> {payload_name}<'a> {{
            {payload_methods_impl}
        }}

        #[repr(u32)]
        #[derive(Clone, Copy, Debug)]
        {vis} enum {tag_name} {{
            {tags}
        }}

        {compat_enum}
        "
    )
    .parse()
    .unwrap()
}
