mod engine;
mod error_handler;
mod ffi;
mod types;
mod screen;

#[macro_export]
macro_rules! traceln {
    ($fmt:expr) => (
        // if cfg!(features="trace-call") {
        //     println!($fmt);
        // }
        println!($fmt);
    );
    ($fmt:expr, $($arg:tt)*) => (
        // if cfg!(features="trace-call") {
        //     println!($fmt, $($arg)*);
        // }
        println!($fmt, $($arg)*);
    );
}
