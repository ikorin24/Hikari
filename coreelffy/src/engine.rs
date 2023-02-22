use crate::error_handler::*;
use crate::screen::*;
use crate::*;
use std::error::Error;
use std::fmt::Debug;
use std::sync;
use winit;
use winit::{event, event_loop};

pub(crate) struct Engine {
    pub(crate) event_cleared: ClearedEventFn,
    pub(crate) event_redraw_requested: RedrawRequestedEventFn,
    pub(crate) event_resized: ResizedEventFn,
    pub(crate) event_keyboard: KeyboardEventFn,
    pub(crate) event_char_received: CharReceivedEventFn,
    pub(crate) event_closing: ClosingEventFn,
}

static ENGINE: sync::RwLock<Option<Engine>> = sync::RwLock::new(None);

pub(crate) const SHOULD_NOT_NONE_WHEN_ENGINE_RUNNING: EngineErr =
    EngineErr::new("It should not be None when engine is running");

pub(crate) fn get_callback<T>(f: fn(engine: &Engine) -> T) -> Result<T, EngineErr> {
    let reader = ENGINE.read().unwrap();
    match reader.as_ref().map(f) {
        Some(value) => Ok(value),
        None => Err(SHOULD_NOT_NONE_WHEN_ENGINE_RUNNING),
    }
}

/// Start the engine.
/// The function never returns except error.
pub(crate) fn engine_start(
    engine_config: &EngineCoreConfig,
    screen_config: &HostScreenConfig,
) -> Box<dyn Error> {
    env_logger::init();
    set_err_dispatcher(engine_config.err_dispatcher);
    match ENGINE.write() {
        Ok(mut writer) => {
            *writer = Some(Engine {
                event_cleared: engine_config.event_cleared,
                event_redraw_requested: engine_config.event_redraw_requested,
                event_resized: engine_config.event_resized,
                event_keyboard: engine_config.event_keyboard,
                event_char_received: engine_config.event_char_received,
                event_closing: engine_config.event_closing,
            });
        }
        Err(err) => {
            return Box::new(err);
        }
    };
    let event_loop = event_loop::EventLoop::new();
    let screen = match HostScreen::new(screen_config, &event_loop) {
        Ok(screen) => Box::new(screen),
        Err(err) => {
            return err;
        }
    };
    let screen_info = &screen.get_info();
    let screen_id = {
        let on_screen_init = engine_config.on_screen_init;
        on_screen_init(screen, screen_info)
    };

    let find_screen = engine_config.on_find_screen;

    event_loop.run(move |event, _event_loop, control_flow| {
        if let Some(screen) = unsafe { find_screen(screen_id).as_ref() } {
            if handle_event(screen, &event) == false {
                screen.request_close();
            }

            if screen.reset_close_request() {
                let event_closing = get_callback(|engine| engine.event_closing).unwrap();
                let mut cancel = false;
                event_closing(screen, &mut cancel);
                if cancel == false {
                    *control_flow = winit::event_loop::ControlFlow::Exit;
                }
            }
        }
    });
}

fn handle_event(screen: &HostScreen, event: &event::Event<()>) -> bool {
    use winit::event::*;

    let target_window_id = screen.window.id();

    match event {
        Event::WindowEvent {
            ref event,
            window_id,
        } if *window_id == target_window_id => match event {
            WindowEvent::ReceivedCharacter(c) => {
                let event_char_received =
                    get_callback(|engine| engine.event_char_received).unwrap();
                event_char_received(screen, *c);
            }
            WindowEvent::CloseRequested => {
                return false;
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
                let event_keyboard = get_callback(|engine| engine.event_keyboard).unwrap();
                let pressed = match state {
                    ElementState::Pressed => true,
                    ElementState::Released => false,
                };
                event_keyboard(screen, *key, pressed);
            }
            WindowEvent::Resized(physical_size) => {
                let event_resized = get_callback(|engine| engine.event_resized).unwrap();
                event_resized(screen, physical_size.width, physical_size.height);
            }
            WindowEvent::ScaleFactorChanged { new_inner_size, .. } => {
                let event_resized = get_callback(|engine| engine.event_resized).unwrap();
                event_resized(screen, new_inner_size.width, new_inner_size.height);
            }
            _ => {}
        },
        Event::RedrawRequested(window_id) if *window_id == target_window_id => {
            let event_redraw_requested =
                get_callback(|engine| engine.event_redraw_requested).unwrap();
            event_redraw_requested(screen);
        }
        Event::MainEventsCleared => {
            let event_cleared = get_callback(|engine| engine.event_cleared).unwrap();
            event_cleared(screen);
        }
        _ => {}
    }
    return true;
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
}
