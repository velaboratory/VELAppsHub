<?php

$apps_json = file_get_contents("apps.json");
$apps = json_decode($apps_json, true);

for ($i = 0; $i < count($apps["apps"]); $i++) {
    // thumbnail url
    $apps["apps"][$i]["thumbnail"] = get_current_folder_url() . "static/" . $apps["apps"][$i]["folder"] . "/thumbnail.png";

    // get version
    $folder = getcwd() . "/static/" . $apps["apps"][$i]["folder"] . "/zips";
    $files = scandir($folder, 1);
    if (count($files) > 0) {
        $version = explode(".zip", explode("_v", $files[0])[1])[0];
        $apps["apps"][$i]["version"] = $version;
        $apps["apps"][$i]["download"] = get_current_folder_url() . "/static/" . $apps["apps"][$i]["folder"] . "/zips/" . $files[0];
    }
}

header('Content-type: application/json');
echo json_encode($apps);

function get_current_folder_url()
{
    $protocol = ((!empty($_SERVER['HTTPS']) && $_SERVER['HTTPS'] != 'off') || $_SERVER['SERVER_PORT'] == 443) ? "https://" : "http://";
    return $protocol . $_SERVER['HTTP_HOST'] . dirname($_SERVER['PHP_SELF']) . '/';
}
