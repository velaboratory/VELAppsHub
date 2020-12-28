from flask import Flask, abort, escape, jsonify, render_template, request, Response
import json
import os

app = Flask(__name__)

access_codes = {
    'all': 'all',
    'statics2020': ['vr-statics']
}


@app.route('/access_code', methods=['GET'])
def access_code():
    return "", 500


@app.route('/get_apps', methods=['GET'])
def get_apps():
    prepend_url = request.url_root
    with open('apps.json', 'r') as f:
        apps = json.load(f)['apps']

        for app in apps:
            app['thumbnail'] = os.path.join(
                prepend_url, 'static', app['folder'], 'thumbnail.png')

            # get version
            folder = os.path.join('static', app['folder'], 'zips')
            files = os.listdir(folder)
            paths = [os.path.join(folder, basename)
                     for basename in files]
            latest_file = max(paths, key=os.path.getctime)
            version = latest_file.split('_v')[1].split('.zip')[0]

            app['version'] = version
            app['download'] = os.path.join(prepend_url, latest_file)

    return jsonify({'apps': apps})
