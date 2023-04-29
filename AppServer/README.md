# VEL Apps Hub Server

Holds all the application binaries and serves them if the correct access code is supplied

## Running locally

1. Create a python environment
```
python -m venv env
. env/bin/activate
```

2. Install packages
```
pip install -r requirements.txt
```

3. Create the `static` folder
```
mkdir static
```

4. Run the server
```
uvicorn server:app --reload
```
