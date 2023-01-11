use std::num::NonZeroU32;
// use std::panic;
use std::sync::atomic::{AtomicU32, Ordering};
use std::sync::Mutex;

mod engine;
mod ffi;
mod types;

static ERR_DISPATCHER: Mutex<Option<DispatchErrFn>> = Mutex::new(None);

// pub(crate) fn panic_safe_call<F: FnOnce() -> R + std::panic::UnwindSafe, R>(
//     f: F,
// ) -> Result<R, ErrMessageId> {
//     panic::catch_unwind(f).map_err(|err| {
//         let id = generate_message_id();
//         let message = format!("{:?}", err);

//         if let Some(dispatch_err) = get_err_dispatcher() {
//             let message_bytes = message.as_bytes();
//             dispatch_err(id, message_bytes.as_ptr(), message_bytes.len());
//         } else {
//             eprintln!("{}", message);
//         }
//         ErrMessageId(id)
//     })
// }

pub(crate) fn set_err_dispatcher(dispatcher: DispatchErrFn) {
    match ERR_DISPATCHER.lock() {
        Ok(mut value) => {
            *value = Some(dispatcher);
        }
        Err(err) => {
            eprintln!(
                "could not set an error dispatcher because mutex is poisoned for some reason: {:?}",
                err
            );
        }
    }
}

fn get_err_dispatcher() -> Option<DispatchErrFn> {
    match ERR_DISPATCHER.try_lock() {
        Ok(f) => *f,
        Err(_) => None,
    }
}

fn generate_message_id() -> NonZeroU32 {
    static ID_SEED: AtomicU32 = AtomicU32::new(1);

    // x  = { 1, 3, 5, ..., 2^32-1 }
    // id = x / 2 + 1
    //    = { 1, 2, 3, ..., 2^31 }
    let x = ID_SEED.fetch_add(2, Ordering::Relaxed) / 2 + 1;
    unsafe { NonZeroU32::new_unchecked(x / 2 + 1) }
}

#[repr(transparent)]
pub(crate) struct ApiResult(u32);

impl From<Result<(), ErrMessageId>> for ApiResult {
    #[inline]
    fn from(x: Result<(), ErrMessageId>) -> Self {
        match x {
            Ok(_) => ApiResult(0),
            Err(ErrMessageId(id)) => ApiResult(id.into()),
        }
    }
}

#[repr(transparent)]
pub(crate) struct ErrMessageId(NonZeroU32);

pub(crate) type DispatchErrFn =
    extern "cdecl" fn(id: NonZeroU32, message: *const u8, message_len: usize) -> ();
