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
	version?: string;
	download?: string;
};

export type LocalAppState = {
	id: string;
	exe_path: string;
	installed_version: string;
	loading?: boolean;
	progress?: number;
	task?: string;
};

export type AppSettings = {
	access_code: string;
};
