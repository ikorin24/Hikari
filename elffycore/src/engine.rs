use crate::error_handler::*;
use crate::screen::*;
use crate::types::*;
use std::error::Error;
use std::fmt::Debug;
use std::sync;
use winit;
use winit::event_loop;

struct Engine {
    on_command_begin: OnCommandBeginFn,
    on_resized: HostScreenResizedFn,
}

static ENGINE: sync::RwLock<Option<Engine>> = sync::RwLock::new(None);

pub(crate) const SHOULD_NOT_NONE_WHEN_ENGINE_RUNNING: EngineErr =
    EngineErr::new("It should not be None when engine is running");

pub(crate) fn get_callback_command_begin() -> Result<OnCommandBeginFn, EngineErr> {
    let reader = ENGINE.read().unwrap();
    match reader.as_ref().map(|engine| engine.on_command_begin) {
        Some(value) => Ok(value),
        None => Err(SHOULD_NOT_NONE_WHEN_ENGINE_RUNNING),
    }
}

pub(crate) fn get_callback_resized() -> Result<HostScreenResizedFn, EngineErr> {
    let reader = ENGINE.read().unwrap();
    match reader.as_ref().map(|engine| engine.on_resized) {
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
    {
        match ENGINE.write() {
            Ok(mut writer) => {
                *writer = Some(Engine {
                    on_command_begin: engine_config.on_command_begin,
                    on_resized: engine_config.on_resized,
                });
            }
            Err(err) => {
                return Box::new(err);
            }
        };
    }
    let event_loop = event_loop::EventLoop::new();
    let screen = match HostScreen::new(screen_config, &event_loop) {
        Ok(screen) => Box::new(screen),
        Err(err) => {
            return err;
        }
    };
    (engine_config.on_screen_init)(screen.as_ref(), &screen.get_info());
    event_loop.run(move |event, event_loop, control_flow| {
        screen.handle_event(&event, event_loop, control_flow);
    });
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
