import { Feedback, InspectionData, InspectionRecord, inspectionRecordToInspectionData } from 'models/InspectionRecord'
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

// SARA defaults to 25 records per page and applies no upper bound, so wide time ranges must be
// paged through to avoid silently plotting a truncated series. The page cap is only a runaway
// guard for a server that misreports totalPages; it is not expected to be reached.
const inspectionRecordPageSize = 200
const maxInspectionRecordPages = 50

export class SaraApi {
    constructor(private readonly api: BackendAPICaller) {}

    async getSaraDataByInspectionId(inspectionId: string): Promise<InspectionData> {
        const path: string = 'api/inspection-record/inspection-id/' + inspectionId

        const content = this.api
            .GET<InspectionRecord>(path)
            .then((response) => {
                if (!response.content.analyses || response.content.analyses.length < 1) throw Error('No analysis found')
                const data = inspectionRecordToInspectionData(response.content)
                if (!data) throw Error('No analysis found')
                return data
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
        const parameters = new URLSearchParams()

        inspectionIds?.forEach((inspectionId) => parameters.append('InspectionIds', inspectionId))
        if (installationCode) parameters.append('InstallationCode', installationCode)
        if (tagId) parameters.append('Tag', tagId)
        if (analysisType) parameters.append('AnalysisTypes', analysisType)
        if (minDate) parameters.append('MinCreationTime', minDate.toISOString())
        if (maxDate) parameters.append('MaxCreationTime', maxDate.toISOString())
        parameters.set('PageSize', String(inspectionRecordPageSize))

        const records: InspectionRecord[] = []
        let pageNumber = 1
        let totalPages = 1
        let path = ''

        try {
            do {
                parameters.set('PageNumber', String(pageNumber))
                path = 'api/inspection-record?' + parameters.toString()

                const response = await this.api.GET<PaginatedInspectionRecords>(path)

                if (!response.content) throw Error('No inspection records found')

                records.push(...response.content.items)
                totalPages = response.content.totalPages
                pageNumber++
            } while (pageNumber <= Math.min(totalPages, maxInspectionRecordPages))
        } catch (e) {
            return handleError('GET', path)(e)
        }

        if (totalPages > maxInspectionRecordPages)
            console.error(
                'Inspection record query spans %d pages, only the first %d were loaded',
                totalPages,
                maxInspectionRecordPages
            )

        return records.map((r) => inspectionRecordToInspectionData(r)).filter((r) => r !== null)
    }

    async upsertFeedback(analysisRunId: string, isCorrect: boolean): Promise<Feedback> {
        const path: string = 'api/feedback/analysis-run/' + analysisRunId

        const content = this.api
            .PUT<{ isCorrect: boolean }, Feedback>(path, { isCorrect })
            .then((response) => response.content)
            .catch(handleError('PUT', path))
        return content
    }

    async deleteFeedback(analysisRunId: string): Promise<void> {
        const path: string = 'api/feedback/analysis-run/' + analysisRunId

        await this.api.DELETE(path).catch(handleError('DELETE', path))
    }
}
