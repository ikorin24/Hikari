use crate::screen::*;
use crate::*;
use once_cell::sync::Lazy;
use regex::Regex;
use std::cell::Cell;
use std::error::Error;
use std::fmt::Debug;
use std::num::NonZeroUsize;
use std::sync;
use std::sync::atomic::AtomicUsize;
use std::sync::atomic::Ordering;
use std::sync::Mutex;
use winit;
use winit::event_loop::{EventLoopBuilder, EventLoopProxy, EventLoopWindowTarget};
use winit::platform::run_return::EventLoopExtRunReturn;
use winit::{event, window};

pub(crate) struct Engine {
    config: EngineCoreConfig,
}

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

#[inline]
fn generate_message_id() -> ErrMessageId {
    static ID_SEED: AtomicUsize = AtomicUsize::new(1);

    // x  = { 1, 3, 5, ..., 2^N -1 }
    // id = x / 2 + 1
    //    = { 1, 2, 3, ..., 2^(N-1) }
    let x = ID_SEED.fetch_add(2, Ordering::Relaxed) / 2 + 1;
    ErrMessageId(unsafe { NonZeroUsize::new_unchecked(x / 2 + 1) })
}

impl Engine {
    pub fn new(config: &EngineCoreConfig) -> Self {
        Engine { config: *config }
    }

    pub fn dispatch_err(err: impl std::fmt::Display) {
        static ANCI_ESC_SEQ_DECORATION: Lazy<Regex> =
            Lazy::new(|| Regex::new("\x1b\\[[0-9;]*m").unwrap());

        let message = format!("{}", err);

        // Some error messages contain decorations of ANSI escape sequences.
        // They should be removed.
        let message = ANCI_ESC_SEQ_DECORATION.replace_all(&message, "");

        increment_tls_err_count();
        let id = generate_message_id();
        if let Some(err_dispatcher) =
            Self::get_engine_field(|engine| engine.config.err_dispatcher).ok()
        {
            let message_bytes = message.as_bytes();
            err_dispatcher(id, message_bytes.as_ptr(), message_bytes.len());
        } else {
            eprintln!("id: {}, {}", id, message);
        }
    }

    pub fn on_screen_init(screen: Box<HostScreen>) -> ScreenId {
        let f = Self::get_engine_field(|engine| engine.config.on_screen_init).unwrap();
        let screen_info = &screen.get_info();
        f(screen, screen_info)
    }

    pub fn event_cleared(screen_id: ScreenId) {
        let f = Self::get_engine_field(|engine| engine.config.event_cleared).unwrap();
        f(screen_id)
    }

    pub fn event_redraw_requested(screen_id: ScreenId) -> bool {
        let f = Self::get_engine_field(|engine| engine.config.event_redraw_requested).unwrap();
        f(screen_id)
    }

    pub fn event_resized(screen_id: ScreenId, width: u32, height: u32) {
        let f = Self::get_engine_field(|engine| engine.config.event_resized).unwrap();
        f(screen_id, width, height)
    }

    pub fn event_keyboard(
        screen_id: ScreenId,
        key: &event::VirtualKeyCode,
        state: &event::ElementState,
    ) {
        let f = Self::get_engine_field(|engine| engine.config.event_keyboard).unwrap();
        let pressed = match state {
            event::ElementState::Pressed => true,
            event::ElementState::Released => false,
        };
        f(screen_id, *key, pressed)
    }

    pub fn event_char_received(screen_id: ScreenId, c: char) {
        let f = Self::get_engine_field(|engine| engine.config.event_char_received).unwrap();
        f(screen_id, c)
    }

    pub fn event_ime(screen_id: ScreenId, input: &ImeInputData) {
        let f = Self::get_engine_field(|engine| engine.config.event_ime).unwrap();
        f(screen_id, input)
    }

    pub fn event_wheel(screen_id: ScreenId, x_delta: f32, y_delta: f32) {
        let f = Self::get_engine_field(|engine| engine.config.event_wheel).unwrap();
        f(screen_id, x_delta, y_delta)
    }

    pub fn event_cursor_moved(screen_id: ScreenId, x: f32, y: f32) {
        let f = Self::get_engine_field(|engine| engine.config.event_cursor_moved).unwrap();
        f(screen_id, x, y)
    }

    pub fn event_cursor_entered_left(screen_id: ScreenId, entered: bool) {
        let f = Self::get_engine_field(|engine| engine.config.event_cursor_entered_left).unwrap();
        f(screen_id, entered);
    }

    pub fn event_closing(screen_id: ScreenId) -> bool {
        let f = Self::get_engine_field(|engine| engine.config.event_closing).unwrap();
        let mut cancel = false;
        f(screen_id, &mut cancel);
        !cancel
    }

    pub fn event_closed(screen_id: ScreenId) -> Option<Box<HostScreen>> {
        let f = Self::get_engine_field(|engine| engine.config.event_closed).unwrap();
        f(screen_id)
    }

    fn get_engine_field<T>(f: fn(engine: &Engine) -> T) -> Result<T, EngineErr> {
        let reader = ENGINE.read().unwrap();
        match reader.as_ref().map(f) {
            Some(value) => Ok(value),
            None => Err(EngineErr::NOT_RUNNING),
        }
    }
}

static ENGINE: sync::RwLock<Option<Engine>> = sync::RwLock::new(None);

static SCREENS: Mutex<Vec<ScreenIdData>> = Mutex::new(vec![]);

static LOOP_PROXY: Mutex<Option<EventLoopProxy<ProxyMessage>>> = Mutex::new(None);

#[derive(Debug, Clone, Copy)]
pub(crate) enum ProxyMessage {
    CreateScreen(HostScreenConfig),
}

pub(crate) fn get_loop_proxy() -> Result<EventLoopProxy<ProxyMessage>, EngineErr> {
    let proxy = LOOP_PROXY.lock().unwrap();
    proxy.clone().ok_or(EngineErr::NOT_RUNNING)
}

fn get_screen(window_id: &window::WindowId) -> Option<ScreenIdData> {
    let screens = SCREENS.lock().unwrap();
    screens.iter().find(|x| x.0 == *window_id).copied()
}
fn push_screen(id_data: ScreenIdData) {
    let mut screens = SCREENS.lock().unwrap();
    screens.push(id_data);
}
fn remove_screen(id_data: ScreenIdData, is_empty: &mut bool) {
    let mut screens = SCREENS.lock().unwrap();
    let index = screens
        .iter()
        .enumerate()
        .find(|(_, x)| **x == id_data)
        .map(|(i, _)| i);
    if let Some(index) = index {
        screens.swap_remove(index);
    }
    *is_empty = screens.is_empty();
}

#[derive(Clone, Copy, PartialEq, Eq, Hash)]
struct ScreenIdData(window::WindowId, ScreenId);

pub(crate) fn send_proxy_message(message: ProxyMessage) -> Result<(), Box<dyn Error>> {
    let proxy = get_loop_proxy()?;
    proxy.send_event(message)?;
    Ok(())
}

fn create_screen(
    screen_config: &HostScreenConfig,
    event_loop: &EventLoopWindowTarget<ProxyMessage>,
) -> Result<(), Box<dyn Error>> {
    let screen = Box::new(HostScreen::new(screen_config, event_loop)?);
    let window_id = screen.window.id();
    let screen_id = Engine::on_screen_init(screen);
    push_screen(ScreenIdData(window_id, screen_id));
    Ok(())
}

pub(crate) fn engine_start(
    engine_config: &EngineCoreConfig,
    screen_config: &HostScreenConfig,
) -> Result<(), Box<dyn Error>> {
    let is_engine_running = {
        let engine = ENGINE.read().unwrap();
        engine.is_some()
    };
    if is_engine_running {
        return Err(EngineErr::ALREADY_RUNNING.into());
    }
    let mut event_loop = EventLoopBuilder::with_user_event().build();
    {
        let mut engine = ENGINE.write().unwrap();
        *engine = Some(Engine::new(engine_config));
        {
            let mut proxy = LOOP_PROXY.lock().unwrap();
            *proxy = Some(event_loop.create_proxy());
        }
    }
    env_logger::init();
    create_screen(screen_config, &event_loop)?;

    if cfg!(target_os = "windows") {
        // [NOTE]
        // To avoid strange IME behavior, set 'control_flow' to 'Wait' for the first few times.
        // After that, set it to 'Poll'.
        // Poll' is necessary because the game engine always needs to loop as fast as possible.

        let mut i: usize = 0;
        const WAIT_EVENT_COUNT: usize = 50;
        event_loop.run_return(move |event, event_loop, control_flow| {
            if i == 0 {
                control_flow.set_wait();
            }
            if i == WAIT_EVENT_COUNT {
                control_flow.set_poll();
            }
            i = i.saturating_add(1);
            handle_event(&event, event_loop, control_flow);
        });
    } else {
        event_loop.run_return(move |event, event_loop, control_flow| {
            handle_event(&event, event_loop, control_flow);
        });
    }

    // drop the engine from the static field.
    {
        let mut engine = ENGINE.write().unwrap();
        _ = engine.take();
    }
    return Ok(());
}

fn handle_event(
    event: &event::Event<ProxyMessage>,
    event_loop: &EventLoopWindowTarget<ProxyMessage>,
    control_flow: &mut winit::event_loop::ControlFlow,
) {
    use winit::event::*;

    match event {
        Event::UserEvent(proxy_message) => match proxy_message {
            ProxyMessage::CreateScreen(config) => {
                if let Err(err) = create_screen(config, event_loop) {
                    Engine::dispatch_err(err);
                }
            }
        },
        Event::WindowEvent { event, window_id } => {
            if let Some(target) = get_screen(window_id) {
                match event {
                    WindowEvent::CursorEntered { .. } => {
                        Engine::event_cursor_entered_left(target.1, true);
                    }
                    WindowEvent::CursorLeft { .. } => {
                        Engine::event_cursor_entered_left(target.1, false);
                    }
                    WindowEvent::CursorMoved { position, .. } => {
                        Engine::event_cursor_moved(target.1, position.x as f32, position.y as f32);
                    }
                    WindowEvent::Ime(ime) => {
                        let data = ImeInputData::new(ime);
                        Engine::event_ime(target.1, &data);
                    }
                    WindowEvent::ReceivedCharacter(c) => {
                        Engine::event_char_received(target.1, *c);
                    }
                    WindowEvent::MouseWheel { delta, phase, .. } if *phase == TouchPhase::Moved => {
                        match delta {
                            MouseScrollDelta::LineDelta(x_delta, y_delta) => {
                                Engine::event_wheel(target.1, *x_delta, *y_delta);
                            }
                            MouseScrollDelta::PixelDelta(_pos) => {
                                // TODO: support touchpad devices
                            }
                        }
                    }
                    WindowEvent::CloseRequested => {
                        close_screen(&target, control_flow);
                    }
                    WindowEvent::KeyboardInput {
                        input:
                            KeyboardInput {
                                state,
                                virtual_keycode: Some(key),
                                ..
                            },
                        ..
                    } => {
                        Engine::event_keyboard(target.1, key, state);
                    }
                    WindowEvent::Resized(physical_size) => {
                        Engine::event_resized(target.1, physical_size.width, physical_size.height);
                    }
                    WindowEvent::ScaleFactorChanged { new_inner_size, .. } => {
                        Engine::event_resized(
                            target.1,
                            new_inner_size.width,
                            new_inner_size.height,
                        );
                    }
                    _ => {}
                }
            }
        }
        Event::RedrawRequested(window_id) => {
            if let Some(target) = get_screen(window_id) {
                let continue_next = Engine::event_redraw_requested(target.1);
                if continue_next == false {
                    close_screen(&target, control_flow);
                }
            }
        }
        Event::MainEventsCleared => {
            let screens = SCREENS.lock().unwrap();
            screens.iter().for_each(|x| {
                Engine::event_cleared(x.1);
            });
        }
        _ => {}
    }
}

fn close_screen(target: &ScreenIdData, control_flow: &mut winit::event_loop::ControlFlow) {
    if Engine::event_closing(target.1) {
        let mut is_empty = false;
        remove_screen(*target, &mut is_empty);
        let closed_screen = Engine::event_closed(target.1);
        drop(closed_screen);

        if is_empty {
            *control_flow = winit::event_loop::ControlFlow::Exit;
        }
    }
}

#[derive(Clone, Copy)]
pub(crate) struct EngineErr {
    message: &'static str,
}

impl std::fmt::Display for EngineErr {
    fn fmt(&self, f: &mut std::fmt::Formatter<'_>) -> std::fmt::Result {
        write!(f, "{}", self.message)
    }
}

impl std::error::Error for EngineErr {
    fn source(&self) -> Option<&(dyn Error + 'static)> {
        None
    }

    fn description(&self) -> &str {
        "description() is deprecated; use Display"
    }

    fn cause(&self) -> Option<&dyn Error> {
        self.source()
    }
}

impl Debug for EngineErr {
    fn fmt(&self, f: &mut std::fmt::Formatter<'_>) -> std::fmt::Result {
        f.debug_struct("EngineErr")
            .field("message", &self.message)
            .finish()
    }
}

impl EngineErr {
    pub const fn new(message: &'static str) -> Self {
        Self { message }
    }

    const NOT_RUNNING: Self = Self::new("The engine is not running");
    const ALREADY_RUNNING: Self = Self::new("The engine is already running");
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
