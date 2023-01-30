use crate::error_handler::*;
use crate::screen::*;
use crate::types::*;
use std::error::Error;
use std::fmt::Debug;
use std::sync;
use winit;
use winit::{event, event_loop};

pub(crate) struct Engine {
    pub(crate) event_cleared: ClearedEventFn,
    pub(crate) event_redraw_requested: RedrawRequestedEventFn,
    pub(crate) event_resized: ResizedEventFn,
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
    let screen_id = HostScreenId::new(&screen);
    let screen_info = &screen.get_info();
    (engine_config.on_screen_init)(screen, screen_info, screen_id);
    event_loop.run(move |event, _event_loop, control_flow| {
        handle_event(&screen_id, &event, control_flow);
    });
}

fn handle_event(
    screen_id: &HostScreenId,
    event: &event::Event<()>,
    control_flow: &mut event_loop::ControlFlow,
) {
    use winit::event::*;

    let target_window_id = screen_id.window_id();
    match event {
        Event::WindowEvent {
            ref event,
            window_id,
        } if *window_id == target_window_id => match event {
            WindowEvent::CloseRequested
            | WindowEvent::KeyboardInput {
                input:
                    KeyboardInput {
                        state: ElementState::Pressed,
                        virtual_keycode: Some(VirtualKeyCode::Escape),
                        ..
                    },
                ..
            } => *control_flow = winit::event_loop::ControlFlow::Exit,
            WindowEvent::KeyboardInput {
                input:
                    KeyboardInput {
                        state: ElementState::Pressed,
                        virtual_keycode: Some(VirtualKeyCode::Space),
                        ..
                    },
                ..
            } => {}
            WindowEvent::Resized(physical_size) => {
                let event_resized = get_callback(|engine| engine.event_resized).unwrap();
                event_resized(*screen_id, physical_size.width, physical_size.height);
            }
            WindowEvent::ScaleFactorChanged { new_inner_size, .. } => {
                let event_resized = get_callback(|engine| engine.event_resized).unwrap();
                event_resized(*screen_id, new_inner_size.width, new_inner_size.height);
            }
            _ => {}
        },
        Event::RedrawRequested(window_id) if *window_id == target_window_id => {
            let event_redraw_requested =
                get_callback(|engine| engine.event_redraw_requested).unwrap();
            event_redraw_requested(*screen_id);
        }
        Event::MainEventsCleared => {
            let event_cleared = get_callback(|engine| engine.event_cleared).unwrap();
            event_cleared(*screen_id);
        }
        _ => {}
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
}
