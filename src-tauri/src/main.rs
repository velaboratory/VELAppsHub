#![cfg_attr(
    all(not(debug_assertions), target_os = "windows"),
    windows_subsystem = "windows"
)]

extern crate directories;
use std::{fs, path::Path};

use directories::BaseDirs;

fn main() {
    tauri::Builder::default()
        .invoke_handler(tauri::generate_handler![open_installs_folder])
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


