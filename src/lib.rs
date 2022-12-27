mod host_screen;

type ErrorCallback = extern "cdecl" fn(*const u8, usize) -> ();

#[no_mangle]
pub extern "cdecl" fn start(on_err: Option<ErrorCallback>) {
    let on_err = on_err.unwrap();
    match host_screen::start() {
        Ok(_) => {}
        Err(err) => {
            let err_string = err.to_string();
            on_err(err_string.as_ptr(), err_string.len());
        }
    }
}
