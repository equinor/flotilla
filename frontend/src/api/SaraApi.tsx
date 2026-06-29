import { InspectionRecord } from 'models/InspectionRecord'
import { BackendAPICaller } from './ApiCaller'
import { handleError } from './ApiError'

export class SaraApi {
    constructor(private readonly api: BackendAPICaller) {}

    async getAnonymizedDataUrl(inspectionId: string): Promise<string> {
        const path: string = 'api/inspection-record/inspection-id/' + inspectionId

        const content = this.api
            .GET<InspectionRecord>(path)
            .then((response) => {
                if (!response.content.analyses || response.content.analyses.length < 1) throw Error('No analysis found')
                return response.content.analyses[response.content.analyses.length - 1].anonymizedSAS
            })
            .catch(handleError('GET', path))
        return content
    }

    async getAnalysedDataUrl(inspectionId: string): Promise<string> {
        const path: string = 'api/inspection-record/inspection-id/' + inspectionId

        const content = this.api
            .GET<InspectionRecord>(path)
            .then((response) => {
                if (!response.content.analyses || response.content.analyses.length < 1) throw Error('No analysis found')
                return response.content.analyses[response.content.analyses.length - 1].visualizedSAS
            })
            .catch(handleError('GET', path))
        return content
    }
}
