use std::cell::Cell;
use std::fmt::Debug;
use std::num::NonZeroUsize;
use std::sync::atomic::{AtomicUsize, Ordering};
use std::sync::Mutex;

static ERR_DISPATCHER: Mutex<Option<DispatchErrFn>> = Mutex::new(None);

thread_local! {
    /// thread local error counter
    static ERROR_COUNTER: Cell<usize>  = Cell::new(0);
}

/// increment the error counter and get the incremented value.
#[inline]
fn increment_tls_err_count() -> usize {
    ERROR_COUNTER.with(|cell| cell.replace(cell.get() + 1)) + 1
}

/// reset the error counter and get the value before reset.
#[inline]
pub(crate) fn reset_tls_err_count() -> usize {
    ERROR_COUNTER.with(|cell| cell.replace(0))
}

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

pub(crate) fn dispatch_err(err: impl std::fmt::Display) {
    let message = format!("{}", err);
    increment_tls_err_count();
    let id = generate_message_id();
    if let Some(err_dispatcher) = get_err_dispatcher() {
        let message_bytes = message.as_bytes();
        err_dispatcher(id, message_bytes.as_ptr(), message_bytes.len());
    } else {
        eprintln!("id: {}, {}", id, message);
    }
}

fn get_err_dispatcher() -> Option<DispatchErrFn> {
    match ERR_DISPATCHER.try_lock() {
        Ok(f) => *f,
        Err(_) => None,
    }
}

#[inline]
fn generate_message_id() -> ErrMessageId {
    static ID_SEED: AtomicUsize = AtomicUsize::new(1);

    // x  = { 1, 3, 5, ..., 2^N -1 }
    // id = x / 2 + 1
    //    = { 1, 2, 3, ..., 2^(N-1) }
    let x = ID_SEED.fetch_add(2, Ordering::Relaxed) / 2 + 1;
    ErrMessageId(unsafe { NonZeroUsize::new_unchecked(x / 2 + 1) })
}

#[repr(transparent)]
pub(crate) struct ApiResult {
    #[allow(dead_code)]
    err_count: usize,
}

impl ApiResult {
    #[inline]
    pub const fn from_err_count(err_count: usize) -> Self {
        Self { err_count }
    }
}

#[repr(C)]
pub(crate) struct ApiBoxResult<T> {
    err_count: usize,
    value: Option<Box<T>>,
}

impl<T> ApiBoxResult<T> {
    #[inline]
    pub const fn ok(value: Box<T>) -> Self {
        Self {
            err_count: 0,
            value: Some(value),
        }
    }

    #[inline]
    pub fn err(err_count: NonZeroUsize) -> Self {
        Self {
            err_count: err_count.into(),
            value: None,
        }
    }
}

#[repr(C)]
pub(crate) struct ApiValueResult<T: Default + 'static> {
    err_count: usize,
    value: T,
}

impl<T: Default> ApiValueResult<T> {
    #[inline]
    pub const fn ok(value: T) -> Self {
        Self {
            err_count: 0,
            value,
        }
    }
    #[inline]
    pub fn err(err_count: NonZeroUsize) -> Self {
        Self {
            err_count: err_count.into(),
            value: Default::default(),
        }
    }
}

#[repr(transparent)]
#[derive(Clone, Copy, Debug)]
pub(crate) struct ErrMessageId(NonZeroUsize);

impl std::fmt::Display for ErrMessageId {
    fn fmt(&self, f: &mut std::fmt::Formatter<'_>) -> std::fmt::Result {
        write!(f, "{}", self.0)
    }
}

pub(crate) type DispatchErrFn =
    extern "cdecl" fn(id: ErrMessageId, message: *const u8, message_len: usize) -> ();
