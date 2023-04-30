from enum import Enum
from typing import Annotated
import aiofiles
from fastapi import Depends, FastAPI, File, Form, Request, Response, UploadFile, status
from fastapi.responses import FileResponse, RedirectResponse
from fastapi.staticfiles import StaticFiles
from fastapi.middleware.cors import CORSMiddleware
from fastapi.templating import Jinja2Templates
from fastapi.security import HTTPBasic, HTTPBasicCredentials
import json
import os
import secrets

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


templates = Jinja2Templates(directory="templates")

security = HTTPBasic()


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


@app.get('/')
async def main(request: Request, response: Response, credentials: HTTPBasicCredentials = Depends(security)):
    if credentials.username == secrets.username and credentials.password == secrets.password:
        with open('apps.json', 'r') as f:
            apps = json.load(f)['apps']
            return templates.TemplateResponse("index.html", {"request": request, "apps": apps})
    else:
        print('no creds')
        response.status_code = 401
        return


@app.post("/upload")
async def create_upload_file(
    file: UploadFile = File(),
    folder: str = Form(...),
    version: str = Form(...),
    platform: str = Form(...),
):
    print(folder)
    f = os.path.join('static', folder, 'zips', platform)
    os.makedirs(f, exist_ok=True)
    async with aiofiles.open(os.path.join(f, f'{folder}_v{version}.zip'), 'wb') as out_file:
        while content := await file.read(1024):  # async read chunk
            await out_file.write(content)  # async write chunk
    return RedirectResponse("/")


@app.post("/create_app")
async def create_app(
    name: str = Form(...),
    description: str = Form(...),
    folder: str = Form(...),
    accessible_by: str = Form(...),
):
    apps = []
    with open('apps.json', 'r') as f:
        apps = json.load(f)['apps']
        a = {}
        for app in apps:
            if app['folder'] == folder:
                apps.remove(app)
                break
        a['folder'] = folder
        a['name'] = name
        a['description'] = description
        a['accessible_by'] = [a.strip() for a in accessible_by.split(',')]
        apps.append(a)

    with open('apps.json', 'w') as f:
        json.dump({'apps': apps}, f, indent=4)

    return RedirectResponse(url="/", status_code=status.HTTP_302_FOUND)


@app.get("/favicon.ico")
async def favicon():
    return FileResponse('favicon.png')
