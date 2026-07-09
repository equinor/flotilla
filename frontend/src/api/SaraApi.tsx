import { InspectionData, InspectionRecord, inspectionRecordToInspectionData } from 'models/InspectionRecord'
import { BackendAPICaller } from './ApiCaller'
import { handleError } from './ApiError'
import { AnalysisType } from 'models/MissionDefinition'

interface PaginatedInspectionRecords {
    items: InspectionRecord[]
    pageNumber: number
    pageSize: number
    totalCount: number
    totalPages: number
    hasNext: boolean
    hasPrevious: boolean
}

export class SaraApi {
    constructor(private readonly api: BackendAPICaller) {}

    async getSaraDataByInspectionId(inspectionId: string): Promise<InspectionData> {
        const path: string = 'api/inspection-record/inspection-id/' + inspectionId

        const content = this.api
            .GET<InspectionRecord>(path)
            .then((response) => {
                if (!response.content.analyses || response.content.analyses.length < 1) throw Error('No analysis found')
                return inspectionRecordToInspectionData(response.content)
            })
            .catch(handleError('GET', path))
        return content
    }

    async getSaraData(
        inspectionIds?: string[] | null,
        installationCode?: string | null,
        tagId?: string | null,
        analysisType?: AnalysisType | null,
        minDate?: Date | null,
        maxDate?: Date | null
    ): Promise<InspectionData[]> {
        let path: string = 'api/inspection-record?'

        if (inspectionIds) path = path + inspectionIds.map((i) => 'InspectionIds=' + i).join('&')
        if (installationCode) path = path + 'InstallationCode=' + installationCode + '&'
        if (tagId) path = path + 'Tag=' + tagId + '&'
        if (analysisType) path = path + 'AnalysisTypes=' + [analysisType] + '&'
        if (minDate) path = path + 'MinCreationTime=' + minDate.toISOString() + '&'
        if (maxDate) path = path + 'MaxCreationTime=' + maxDate

        const content = this.api
            .GET<PaginatedInspectionRecords>(path)
            .then((response) => {
                if (!response.content) throw Error('No inspection records found')

                return response.content.items.map((r) => inspectionRecordToInspectionData(r))
            })
            .catch(handleError('GET', path))
        return content
    }
}
