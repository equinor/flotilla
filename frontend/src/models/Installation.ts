export enum DataViewName {
    CLOE = 'CLOE',
    FENCILLA = 'FENCILLA',
    THERMAL_READING = 'THERMAL_READING',
}

export interface Installation {
    id: string
    name: string
    installationCode: string
    relevantDataViews: DataViewName[]
}

export const placeholderInstallation: Installation = {
    id: 'placeholderInstallationId',
    name: 'placeholderInstallationName',
    installationCode: 'placeholderInstallationCode',
    relevantDataViews: [],
}
