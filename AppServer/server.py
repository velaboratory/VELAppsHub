from enum import Enum
from fastapi import FastAPI, Request
from fastapi.staticfiles import StaticFiles
from fastapi.middleware.cors import CORSMiddleware
import json
import os

app = FastAPI()

origins = [
    "http://localhost:5173",
]

app.add_middleware(
    CORSMiddleware,
    allow_origins=origins,
    allow_credentials=True,
    allow_methods=["*"],
    allow_headers=["*"],
)

app.mount("/static", StaticFiles(directory="static"), name="static")


class Platform(str, Enum):
    win = "win"
    mac = "mac"
    linux = "linux"


@app.get('/get_apps')
async def get_apps(request: Request, access_code: str, platform: Platform):
    prepend_url = str(request.base_url)
    with open('apps.json', 'r') as f:
        apps = [app for app in json.load(
            f)['apps'] if access_code in app['accessible_by']]

        for app in apps:

            del app['accessible_by']

            # create an accessible url for the thumbnail
            thumbnail_path = os.path.join(
                'static', app['folder'], 'thumbnail.png')
            if os.path.exists(thumbnail_path):
                app['thumbnail'] = f"{prepend_url}static/{app['folder']}/thumbnail.png"

            # get version
            folder = os.path.join('static', app['folder'], 'zips', platform)
            if os.path.exists(folder):
                # find all zip files in the folder, and get version number of the latest one
                files = os.listdir(folder)
                paths = [os.path.join(folder, basename)
                         for basename in files]
                if len(paths) > 0:
                    latest_file = max(paths, key=os.path.getctime)
                    version = latest_file.split('_v')[1].split('.zip')[0]

                    app['version'] = version
                    app['download'] = f"{prepend_url}{latest_file}".replace(
                        '\\', '/')

    return {'apps': apps}
