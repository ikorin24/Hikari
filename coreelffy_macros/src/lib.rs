use proc_macro::TokenStream;
use quote::ToTokens;
use syn::{self, Lit, NestedMeta};
use syn::{parse_macro_input, AttributeArgs};

#[proc_macro_attribute]
pub fn tagged_ref_union(args: TokenStream, input: TokenStream) -> TokenStream {
    let target = parse_macro_input!(input as syn::ItemStruct);
    let name = &target.ident.to_string();
    let elements = parse_macro_input!(args as AttributeArgs)
        .iter()
        .enumerate()
        .map(|(i, arg)| {
            if let NestedMeta::Lit(Lit::Str(s)) = arg {
                let literal = s.value();
                let (elem_name, inner_ty) = literal.split_once("@").unwrap();
                TaggedEnumElement::new(elem_name, inner_ty, i)
            } else {
                panic!("all args must be literal str");
            }
        })
        .collect::<Vec<_>>();
    let tagged_enum = TaggedEnum::new(format!("{name}Enum"), elements);
    let vis = target.vis.to_token_stream().to_string();

    let tag_name = format!("{name}Tag");
    let payload_name = format!("{name}Payload");
    let enum_name = format!("{name}Enum");

    let to_enum_method_impl = {
        let cases = tagged_enum
            .elements
            .iter()
            .map(|elem| {
                let elem_i = elem.i;
                let elem_name = &elem.name;
                format!(
                    r"
            {tag_name}::{elem_name} => {enum_name}::{elem_name}(
                unsafe{{ self.payload.cast{elem_i}() }}
            )"
                )
            })
            .collect::<Vec<_>>()
            .join(",\n");
        format!(
            r"#[inline]
            pub fn to_enum(&self) -> {enum_name}<'a> {{
                match self.tag {{
                    {cases}
                }}
            }}"
        )
    };

    let tags = tagged_enum
        .elements
        .iter()
        .map(|elem| {
            let elem_i = elem.i;
            let elem_name = &elem.name;
            let inner_ty = &elem.inner_ty;
            format!(
                r"/// payload is `&{inner_ty}`
                {elem_name} = {elem_i},
                "
            )
        })
        .collect::<Vec<_>>()
        .join("\n");

    let payload_methods_impl = tagged_enum
        .elements
        .iter()
        .map(|elem| {
            let elem_i = elem.i;
            let inner_ty = &elem.inner_ty;
            format!(
                r"#[inline]
                pub unsafe fn cast{elem_i}(&self) -> &'a {inner_ty} {{
                    self.ptr.cast().as_ref()
                }}"
            )
        })
        .collect::<Vec<_>>()
        .join("\n");

    let tagged_enum_source = tagged_enum.to_source(&vis);
    let source = format!(
        r"
        #[repr(C)]
        {vis} struct {name}<'a> {{
            tag: {tag_name},
            payload: {payload_name}<'a>,
        }}

        impl<'a> {name}<'a> {{
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
        #[derive(Debug, PartialEq, Eq, Clone, Copy)]
        {vis} enum {tag_name} {{
            {tags}
        }}

        {tagged_enum_source}
        "
    );
    // panic!("{source}");
    source.parse().unwrap()
}

struct TaggedEnumElement {
    name: String,
    inner_ty: String,
    i: usize,
}

impl TaggedEnumElement {
    pub fn new(name: &str, inner_ty: &str, i: usize) -> Self {
        Self {
            name: name.to_owned(),
            inner_ty: inner_ty.to_owned(),
            i,
        }
    }
}

struct TaggedEnum {
    name: String,
    elements: Vec<TaggedEnumElement>,
}

impl TaggedEnum {
    pub fn new(name: String, elements: Vec<TaggedEnumElement>) -> Self {
        Self { name, elements }
    }
    pub fn to_source(&self, vis: &str) -> String {
        let name = &self.name;
        let elemens = self
            .elements
            .iter()
            .map(|elem| {
                let tag_name = &elem.name;
                let ty = &elem.inner_ty;
                format!("{tag_name}(&'a {ty}),\n")
            })
            .collect::<Vec<_>>()
            .join("");
        format!(
            r"{vis} enum {name}<'a> {{
                {elemens}
            }}
        "
        )
    }
}
