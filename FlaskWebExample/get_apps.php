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
            $max_version = "0";
            $max_index = 0;
            for ($file_i = 0; $file_i < count($files); $file_i++) {
                $version = explode(".zip", explode("_v", $files[$file_i])[1])[0];
                if (version_compare($max_version, $version) < 0) {
                    $max_version = $version;
                    $max_index = $file_i;
                }
            }
            $apps["apps"][$i]["version_" . $os_versions[$os_i]] = $max_version;
            $apps["apps"][$i]["download_" . $os_versions[$os_i]] = get_current_folder_url() . "static/" . $apps["apps"][$i]["folder"] . "/" . $os_versions[$os_i] . "/" . $files[$max_index];
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
