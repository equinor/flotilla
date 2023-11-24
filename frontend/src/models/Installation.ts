export interface Installation {
    id: string
    name: string
    installationCode: string
}

export const placeholderInstallation: Installation = {
    id: 'placeholderInstallationId',
    name: 'placeholderInstallationName',
    installationCode: 'placeholderInstallationCode',
}
