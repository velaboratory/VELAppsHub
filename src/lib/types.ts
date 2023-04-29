export type SettingsSchema = {
	accessCode: string;
};

export type AppsResponse = {
	apps: AppData[];
};

export type AppData = {
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
