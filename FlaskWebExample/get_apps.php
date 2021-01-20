<?php

$apps_json = file_get_contents("apps.json");
$apps = json_decode($apps_json, true);

$access_code = $_GET["access_code"];

$apps_arr = array();
for ($i = 0; $i < count($apps["apps"]); $i++) {
    // thumbnail url
    $apps["apps"][$i]["thumbnail"] = get_current_folder_url() . "static/" . $apps["apps"][$i]["folder"] . "/thumbnail.png";

    $os_versions = ["win", "mac", "linux"];

    for ($os_i = 0; $os_i < count($os_versions); $os_i++) {
        $folder = getcwd() . "/static/" . $apps["apps"][$i]["folder"] . "/" . $os_versions[$os_i];
        $files = scandir($folder, 1);
        // if there is a file other than the current folder
        if (count($files) > 1) {
            // get version
            $version = explode(".zip", explode("_v", $files[0])[1])[0];
            $apps["apps"][$i]["version_" . $os_versions[$os_i]] = $version;
            $apps["apps"][$i]["download_" . $os_versions[$os_i]] = get_current_folder_url() . "static/" . $apps["apps"][$i]["folder"] . "/" . $os_versions[$os_i] . "/" . $files[0];
        }
    }

    if (in_array($access_code, $apps["apps"][$i]["accessible_by"])) {
        array_push($apps_arr, $apps["apps"][$i]);
    }
}

$apps["apps"] = $apps_arr;

header('Content-type: application/json');
echo json_encode($apps);

function get_current_folder_url()
{
    $protocol = ((!empty($_SERVER['HTTPS']) && $_SERVER['HTTPS'] != 'off') || $_SERVER['SERVER_PORT'] == 443) ? "https://" : "http://";
    return $protocol . $_SERVER['HTTP_HOST'] . dirname($_SERVER['PHP_SELF']) . '/';
}
