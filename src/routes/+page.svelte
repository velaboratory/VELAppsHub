<script lang="ts">
	import { invoke } from '@tauri-apps/api/tauri';
	import { SettingsManager } from 'tauri-settings';
	import type { AppData, AppsResponse, SettingsSchema } from '../lib/types';

	let accessCode = '';
	let appsData: AppData[] = [];
	const settingsManager = new SettingsManager<SettingsSchema>(
		{
			accessCode: ''
		},
		{
			// options
			fileName: 'customization-settings'
		}
	);

	// checks whether the settings file exists and creates it if not
	// loads the settings if it exists
	settingsManager.initialize().then(async () => {
		// settingsManager.setCache('accessCode', '');
		// at a later time
		await settingsManager.syncCache();
		accessCode = await settingsManager.get('accessCode');
		refreshApps();
	});

	function refreshApps() {
		fetch(`http://127.0.0.1:8000/get_apps?access_code=${accessCode}`)
			.then((r) => r.json())
			.then((r) => {
				console.log(r);
				appsData = r.apps;
			});
	}
</script>

<div class="flex_content">
	<div class="left_sidebar">
		<img id="logo_img" src="/img/vel_logo_white.png" alt="vel logo" />
		<h1>VEL Apps Hub</h1>
		<p>
			Install and update apps made in the Virtual Experience Laboratory at the University of Georgia
		</p>
		<div class="access_code_box">
			<h3>Access Code</h3>
			<p>Enter an access code to make apps available to download.</p>
			<form id="access_code_form">
				<input id="access_code_input" type="text" spellcheck="false" bind:value={accessCode} />
				<button
					on:click={() => {
						console.log(accessCode);
						settingsManager.set('accessCode', accessCode);
						refreshApps();
					}}>Refresh</button
				>
			</form>
		</div>

		<button
			style="margin-top:2em;"
			on:click={() => {
				console.log('open_installs');
				invoke('open_installs_folder', { name: 'test' });
			}}>Open Install Folder</button
		>

		<p id="version" />
	</div>
	<div class="not_left_sidebar">
		<div id="flex_apps">
			{#if appsData}
				{#each appsData as app}
					<div class="app_box">
						<h3>{app.name}</h3>
						<img src={app.thumbnail} class="thumbnail" alt="thumbnail" />
						<div class="buttons_list">
							<button>Open</button>
							<button>Install</button>
							<button>Uninstall</button>
							<progress />
						</div>
					</div>
				{/each}
			{:else}
				<progress />
			{/if}
		</div>
	</div>
</div>

<style lang="scss">
	.hide {
		display: none;
	}

	.flex_content {
		display: flex;
		position: absolute;
		height: 100%;
		overflow: hidden;
	}

	.left_sidebar {
		max-width: 20em;
		min-width: 15em;
		padding: 1em;
		border-right: 0.1em solid black;
		margin-right: 2em;
	}

	.left_sidebar h1 {
		color: #fff;
	}

	.left_sidebar h3 {
		text-align: left;
		margin-top: 2em;
		color: #fff;
	}

	.left_sidebar p {
		text-align: left;
	}

	#logo_img {
		margin: 1em;
		width: 100%;
		max-width: 10em;
	}

	.not_left_sidebar {
		/* box-shadow: black 0 0 1em; */
	}

	#access_code_input {
		width: 8em;
	}

	form input {
		font-size: 1em;
	}

	form input[type='text'] {
		padding: 0.4em;
		background-color: #252525;
		color: #ccc;
		border: 0.1em solid #000;
	}

	form input[type='text']:hover {
		background-color: #303030;
	}

	form input[type='text']:focus {
		border: 0.1em solid #888;
		background-color: #333333;
		outline: none;
	}

	form input[type='submit'],
	button {
		padding: 0.4em 0.6em 0.4em 0.6em;
		color: #ccc;
		background-color: #222;
		border: 0.1em solid black;
		cursor: pointer;
		font-size: 1em;
	}

	form input[type='submit']:hover,
	button:hover {
		background-color: #522;
		border: 0.1em solid #a22;
	}

	form input[type='submit']:focus,
	button:focus {
		outline: none;
	}

	form input[type='submit']:active,
	button:active {
		background-color: #444;
		outline: none;
	}

	#flex_apps {
		display: flex;
		flex-wrap: wrap;
		height: 100%;
		overflow-y: auto;
	}

	#flex_apps .app_box {
		min-width: 12em;
		max-width: 20em;
		width: 10em;
		background-color: #282828;
		box-shadow: 0 0 0.2em black;
		margin: 1em;
		padding: 1em;
		flex-grow: 1;
		flex-shrink: 1;
		overflow: hidden;
		border: 0.1em solid #0000;
	}

	#flex_apps .app_box:hover {
		background-color: #303030;
		border: 0.1em solid #922;
	}

	#flex_apps .app_box:hover .thumbnail {
		transform: scale(1.01);
	}

	#flex_apps h3 {
		margin-top: 0.2em;
	}

	#flex_apps .thumbnail {
		width: 100%;
	}

	progress {
		width: -webkit-fill-available;
		border-radius: 0;
		margin: 0.2em;
	}

	progress::-webkit-progress-bar {
		background: #222;
		border: 0.1em solid #000;
	}

	progress::-webkit-progress-value {
		background: #522;
		border: 0.1em solid #a22;
	}

	#flex_apps progress[value='0'] {
		display: none;
	}

	#flex_apps ul {
		list-style-type: none;
		margin: 0;
		padding: 0;
	}

	#flex_apps li {
		margin: 0.2em;
	}

	#flex_apps a {
		padding: 0.3em;
		display: block;
		text-decoration: none;
		color: #ccc;
		background-color: #222;
		border: 0.1em solid black;
	}

	#flex_apps a:hover {
		background-color: #522;
		border: 0.1em solid #a22;
	}

	#notification {
		position: fixed;
		bottom: 20px;
		left: 20px;
		width: 200px;
		padding: 20px;
		border-radius: 5px;
		background-color: #464646;
		box-shadow: 0 4px 8px 0 rgba(0, 0, 0, 0.2);
		z-index: 1;
	}

	.hidden {
		display: none;
	}

	#modal {
		position: fixed;
		left: 50%;
		top: 50%;
		margin-left: -10em;
		margin-top: -3em;
		width: 20em;
		padding: 1em;
		background-color: #444;
		border: 0.1em solid black;
		box-shadow: 0 0.1em 0.2em 0 rgba(0, 0, 0, 0.2);
		z-index: 1;
	}

	#modal_text {
		/* z-index: 12; */
	}

	#modal_close {
		/* z-index: 12; */
	}

	#modal_background {
		position: fixed;
		top: 0;
		left: 0;
		width: 100%;
		height: 100%;
		margin: 0;
		background-color: #0005;
		z-index: -1;
	}
	.buttons_list {
		display: flex;
		flex-direction: column;
		gap: 0.25em;
		margin-top: 1em;
	}
</style>
