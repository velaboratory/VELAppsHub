#![cfg_attr(
    all(not(debug_assertions), target_os = "windows"),
    windows_subsystem = "windows"
)]

extern crate directories;
use directories::BaseDirs;
use serde::{Deserialize, Serialize};
use std::{fs, path::Path};

fn main() {
    tauri::Builder::default()
        .invoke_handler(tauri::generate_handler![
            open_installs_folder,
            get_installed_app,
            get_settings,
            set_settings
        ])
        // .invoke_handler(tauri::generate_handler![])
        .run(tauri::generate_context!())
        .expect("error while running tauri application");
}

#[tauri::command]
fn open_installs_folder() {
    if let Some(dirs) = BaseDirs::new() {
        println!("{:?}", dirs.data_dir());
        let path = Path::new(dirs.data_dir())
            .join("VELAppsHub")
            .join("Installs");
        match fs::create_dir_all(path.clone()) {
            Ok(()) => open::that(path).unwrap(),
            Err(e) => println!("{:?}", e),
        }
    }
}

fn get_installs_folder() -> String {
    if let Some(dirs) = BaseDirs::new() {
        let path = Path::new(dirs.data_dir())
            .join("VELAppsHub")
            .join("Installs");
        return String::from(path.as_os_str().to_str().unwrap());
    } else {
        return String::from("");
    }
}

#[derive(Serialize)]

struct AppDetails {
    id: String,
    installed_version: String,
    exe_path: String,
}

#[tauri::command]
fn get_installed_app(id: String) -> AppDetails {
    let mut app_details = AppDetails {
        id: id.clone(),
        installed_version: String::from(""),
        exe_path: String::from(""),
    };
    let version_txt = Path::new(get_installs_folder().as_str())
        .join(id)
        .join("velappshub_appversion.txt");

    app_details.installed_version = match fs::read_to_string(version_txt) {
        Ok(val) => val,
        Err(_err) => String::from(""),
    };

    return app_details;
}

#[derive(Serialize, Deserialize)]
struct AppSettings {
    access_code: String,
}

#[tauri::command]
fn get_settings() -> AppSettings {
    println!("get_settings()");
    if let Some(dirs) = BaseDirs::new() {
        let path = Path::new(dirs.data_dir())
            .join("VELAppsHub")
            .join("settings.json");

        let text = match fs::read_to_string(path) {
            Ok(s) => s,
            Err(_err) => {
                return AppSettings {
                    access_code: String::from(""),
                };
            }
        };
        
        let json: AppSettings = match serde_json::from_str(text.as_str()) {
            Ok(json) => json,
            Err(_err) => AppSettings {
                access_code: String::from(""),
            },
        };
        return json;
    } else {
        return AppSettings {
            access_code: String::from(""),
        };
    }
}

#[tauri::command]
fn set_settings(settings: AppSettings) {
    println!("set_settings()");
    if let Some(dirs) = BaseDirs::new() {
        let path = Path::new(dirs.data_dir())
            .join("VELAppsHub")
            .join("settings.json");
        fs::write(path, serde_json::to_string(&settings).unwrap()).unwrap();
    }
}
