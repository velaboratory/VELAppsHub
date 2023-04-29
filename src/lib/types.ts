export type SettingsSchema = {
	accessCode: string;
};

export type AppsResponse = {
	apps: ServerAppData[];
};

export type ServerAppData = {
	name: string;
	description: string;
	folder: string;
	thumbnail: string;
	version_win?: string;
	download_win?: string;
	version_mac?: string;
	download_mac?: string;
	version_linux?: string;
	download_linux?: string;
};

export type LocalAppState = {
	id:string;
	exe_path:string;
	installed_version:string;
}

export type AppSettings = {
	access_code:string;
}