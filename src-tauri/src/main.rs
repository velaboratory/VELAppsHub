#![cfg_attr(
    all(not(debug_assertions), target_os = "windows"),
    windows_subsystem = "windows"
)]

extern crate directories;
use directories::BaseDirs;
use error_chain::error_chain;

use reqwest::header::CONTENT_LENGTH;
use reqwest::StatusCode;
use serde::{Deserialize, Serialize};
use std::io::{Cursor, Read};
use std::process::Command;
use std::str::FromStr;
use std::thread;
use std::{
    fs::{self, File},
    path::Path,
};
use tauri::{Manager, Window};

fn main() {
    tauri::Builder::default()
        .invoke_handler(tauri::generate_handler![
            open_installs_folder,
            get_installed_app,
            get_settings,
            set_settings,
            install_app,
            uninstall_app,
            open_app,
        ])
        // .invoke_handler(tauri::generate_handler![])
        .run(tauri::generate_context!())
        .expect("error while running tauri application");
}

#[tauri::command]
fn open_installs_folder() {
    let path = get_installs_folder();
    match fs::create_dir_all(path.clone()) {
        Ok(()) => open::that(path).unwrap(),
        Err(e) => println!("{:?}", e),
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

#[derive(Serialize, Deserialize, Clone)]
struct AppProgress {
    id: String,
    progress: f32,
    task: String,
}

#[derive(Serialize, Deserialize)]
struct AppDetails {
    id: String,
    installed_version: String,
    exe_path: String,
}

#[derive(Serialize, Deserialize)]
struct AppServerDetails {
    name: String,
    description: String,
    folder: String,
    thumbnail: String,
    version: String,
    download: String,
}

#[tauri::command]
fn get_installed_app(id: String) -> AppDetails {
    let mut app_details = AppDetails {
        id: id.clone(),
        installed_version: String::from(""),
        exe_path: String::from(""),
    };
    let app_folder = Path::new(get_installs_folder().as_str()).join(id);
    let version_txt = app_folder.join("velappshub_appversion.txt");

    app_details.installed_version = match fs::read_to_string(version_txt) {
        Ok(val) => val,
        Err(_err) => String::from(""),
    };

    match std::path::Path::new(app_folder.clone().as_os_str()).read_dir() {
        Ok(folder) => {
            for element in folder {
                let path = element.unwrap().path();
                if let Some(extension) = path.extension() {
                    if extension == "exe" {
                        println!("FOUND: {:?}", path);
                        app_details.exe_path = String::from(path.to_str().unwrap());
                        break;
                    }
                }
            }
        }
        Err(_err) => {}
    }

    return app_details;
}

#[tauri::command]
fn open_app(app: AppDetails) {
    println!("open_app()");
    println!("{:?}", app.exe_path);
    Command::new(app.exe_path).spawn().unwrap();
}

#[derive(Serialize, Deserialize)]
struct VAHSettings {
    access_code: String,
}

#[tauri::command]
fn get_settings() -> VAHSettings {
    println!("get_settings()");
    if let Some(dirs) = BaseDirs::new() {
        let path = Path::new(dirs.data_dir())
            .join("VELAppsHub")
            .join("settings.json");

        let text = match fs::read_to_string(path) {
            Ok(s) => s,
            Err(_err) => {
                return VAHSettings {
                    access_code: String::from(""),
                };
            }
        };

        let json: VAHSettings = match serde_json::from_str(text.as_str()) {
            Ok(json) => json,
            Err(_err) => VAHSettings {
                access_code: String::from(""),
            },
        };
        return json;
    } else {
        return VAHSettings {
            access_code: String::from(""),
        };
    }
}

#[tauri::command]
fn set_settings(settings: VAHSettings) {
    println!("set_settings()");
    if let Some(dirs) = BaseDirs::new() {
        let path = Path::new(dirs.data_dir())
            .join("VELAppsHub")
            .join("settings.json");
        fs::write(path, serde_json::to_string(&settings).unwrap()).unwrap();
    }
}

error_chain! {
    foreign_links {
        Io(std::io::Error);
        Reqwest(reqwest::Error);
        Header(reqwest::header::ToStrError);
    }
}

#[tauri::command]
fn install_app(window: Window, app: AppServerDetails) {
    println!("download_app()");
    if let Some(dirs) = BaseDirs::new() {
        let path = Path::new(dirs.data_dir())
            .join("VELAppsHub")
            .join("Installs");
        window
            .emit(
                "install-progress",
                AppProgress {
                    id: app.folder.clone(),
                    progress: 0.1,
                    task: String::from("Downloading"),
                },
            )
            .unwrap();
        // download_file_chunked(url, path.clone().to_str().unwrap()).unwrap();
        let zip_path = download_file(app.download, app.folder.clone() + ".zip").unwrap();

        let bytes: Vec<u8> = get_file_as_byte_vec(&zip_path);
        println!("extracting...");
        window
            .emit(
                "install-progress",
                AppProgress {
                    id: app.folder.clone(),
                    progress: 0.5,
                    task: String::from("Extracting"),
                },
            )
            .unwrap();

        thread::spawn(move || {
            zip_extract::extract(Cursor::new(bytes), &path.join(app.folder.clone()), false)
                .unwrap();
            fs::write(
                path.join(app.folder.clone())
                    .join("velappshub_appversion.txt"),
                app.version,
            )
            .unwrap();

            println!("done installing");
            window
                .emit(
                    "install-progress",
                    AppProgress {
                        id: app.folder.clone(),
                        progress: 1.0,
                        task: String::from("Done"),
                    },
                )
                .unwrap();
        });
    }
}

#[tauri::command]
fn uninstall_app(app: AppServerDetails) {
    println!("uninstall_app()");
    fs::remove_dir_all(Path::new(get_installs_folder().as_str()).join(app.folder)).unwrap();
    println!("done uninstalling");
}

// https://gist.github.com/giuliano-oliveira/4d11d6b3bb003dba3a1b53f43d81b30d
// TODO progress ^

// https://rust-lang-nursery.github.io/rust-cookbook/web/clients/download.html
fn download_file(url: String, filename: String) -> Result<String> {
    println!("download_file_chunked()");
    if let Some(dirs) = BaseDirs::new() {
        let path = Path::new(dirs.data_dir())
            .join("VELAppsHub")
            .join("Downloads");

        let zip_path = path.join(filename);

        fs::create_dir_all(path.clone())?;

        let client = reqwest::blocking::Client::new();
        let response = client.head(url.clone()).send()?;
        let length = response
            .headers()
            .get(CONTENT_LENGTH)
            .ok_or("response doesn't include the content length")?;
        let _length =
            u64::from_str(length.to_str()?).map_err(|_| "invalid Content-Length header")?;

        let mut output_file = File::create(zip_path.clone())?;

        println!("starting download...");
        let mut response = client.get(url).send()?;

        let status = response.status();
        if !(status == StatusCode::OK || status == StatusCode::PARTIAL_CONTENT) {
            error_chain::bail!("Unexpected server response: {}", status)
        }
        std::io::copy(&mut response, &mut output_file)?;

        let content = response.text()?;
        std::io::copy(&mut content.as_bytes(), &mut output_file)?;

        println!("Finished with success!");

        return Ok(String::from(zip_path.to_str().unwrap()));
    }
    Ok(String::from(""))
}

fn get_file_as_byte_vec(filename: &String) -> Vec<u8> {
    let mut f = File::open(&filename).expect("no file found");
    let metadata = fs::metadata(&filename).expect("unable to read metadata");
    let mut buffer = vec![0; metadata.len() as usize];
    f.read(&mut buffer).expect("buffer overflow");

    buffer
}
