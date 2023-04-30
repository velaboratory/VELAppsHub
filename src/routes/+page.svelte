<script lang="ts">
	import { invoke } from '@tauri-apps/api/tauri';
	import { emit, listen } from '@tauri-apps/api/event';
	import { platform } from '@tauri-apps/api/os';
	import type { ServerAppData, AppsResponse, LocalAppState, AppSettings } from '$lib/types';

	let accessCode = '';
	let currentPlatform: 'win' | 'mac' | 'linux';
	let localAppStates: { [key: string]: LocalAppState } = {};
	let appDefinitions: ServerAppData[] = [];

	invoke('get_settings', {})
		.then((r) => r as AppSettings)
		.then(async (r) => {
			console.log(r);
			accessCode = r.access_code;
			switch (await platform()) {
				case 'win32':
					currentPlatform = 'win';
					break;
				case 'darwin':
					currentPlatform = 'mac';
					break;
				case 'linux':
					currentPlatform = 'linux';
					break;
			}
			refreshApps();
		});

	listen('app-installed', (event) => {
		if (!event.payload) return;
		let p = event.payload as string;
		invoke('get_installed_app', { id: p }).then((r) => {
			localAppStates[p] = r as LocalAppState;
			console.log(r);
		});
	});
	listen('install-progress', (event) => {
		if (!event.payload) return;
		let p = event.payload as { id: string; progress: number; task: string };
		localAppStates[p.id].progress = p.progress;
		localAppStates[p.id].task = p.task;
		if (p.progress == 1) localAppStates[p.id].loading = false;
	});

	function refreshApps() {
		fetch(`http://127.0.0.1:8000/get_apps?access_code=${accessCode}&platform=${currentPlatform}`)
			.then((r) => r.json())
			.then((r: AppsResponse) => {
				appDefinitions = r.apps;
				for (let app of appDefinitions) {
					invoke('get_installed_app', { id: app.folder }).then((r) => {
						localAppStates[app.folder] = r as LocalAppState;
						console.log(r);
					});
				}
			});
	}
	function refreshLocalApps() {
		for (let app of appDefinitions) {
			invoke('get_installed_app', { id: app.folder }).then((r) => {
				localAppStates[app.folder] = r as LocalAppState;
				console.log(r);
			});
		}
	}

	async function install(app: ServerAppData) {
		localAppStates[app.folder].loading = true;
		const ret = await invoke('install_app', { app });
		console.log(ret);
	}

	// async function download_file(url: string, dest: string, options?: RequestOptions) {
	// 	await fs.writeBinaryFile(
	// 		dest,
	// 		(
	// 			await (
	// 				await getClient()
	// 			).get(url, {
	// 				...(options || {}),
	// 				responseType: ResponseType.Binary
	// 			})
	// 		).data as any
	// 	);
	// };

	$: {
		invoke('set_settings', {
			settings: {
				access_code: accessCode
			}
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
				<button on:click={refreshApps}>Refresh</button>
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
			{#if appDefinitions}
				{#each appDefinitions as app}
					<div class="app_box">
						<img src={app.thumbnail} class="thumbnail" alt="thumbnail" />
						<h3>{app.name}</h3>
						<p>{app.description}</p>
						<p>Version: <strong>{app.version ?? '?'}</strong></p>
						{#if localAppStates[app.folder]?.installed_version !== '' && localAppStates[app.folder]?.installed_version !== app.version}
							<p>
								Installed Version: <strong
									>{localAppStates[app.folder]?.installed_version ?? '?'}</strong
								>
							</p>
						{/if}
						<hr />
						<div class="buttons_list">
							{#if localAppStates[app.folder]?.loading}
								<p style="margin:0;">{localAppStates[app.folder]?.task ?? ''}</p>
								<progress value={localAppStates[app.folder]?.progress ?? ''} max="1" />
							{:else}
								{#if localAppStates[app.folder]?.installed_version !== '' && localAppStates[app.folder]?.installed_version !== app.version}
									<button
										on:click={() => {
											install(app);
										}}
									>
										Update
									</button>
								{/if}

								{#if localAppStates[app.folder]?.exe_path !== ''}
									<button
										on:click={() => {
											invoke('open_app', { app: localAppStates[app.folder] });
										}}>Open</button
									>
									<button
										on:click={() => {
											invoke('uninstall_app', { app }).then(refreshApps);
										}}>Uninstall</button
									>
								{:else if app.version != null}
									<button
										on:click={() => {
											install(app);
										}}>Install</button
									>
								{/if}
							{/if}
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
		width: 100%;
		border-radius: 0;
	}

	progress::-webkit-progress-bar {
		background: #222;
		border: 0.1em solid #000;
	}

	progress::-webkit-progress-value {
		background: #522;
		border: 0.1em solid #a22;
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

	.buttons_list {
		display: flex;
		flex-direction: column;
		gap: 0.25em;
		margin-top: 1em;
	}

	hr {
		border: none;
		height: 0.05em;
		background-color: #fff3;
		width: 75%;
	}
</style>
