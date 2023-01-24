mod engine;
mod error;
mod ffi;
mod types;

#[macro_export]
macro_rules! traceln {
    ($fmt:expr) => (
        if cfg!(features="trace-call") {
            println!($fmt);
        }
    );
    ($fmt:expr, $($arg:tt)*) => (
        if cfg!(features="trace-call") {
            println!($fmt, $($arg)*);
        }
    );
}
