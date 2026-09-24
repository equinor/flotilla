import { useSearchParams } from 'react-router'
import { ResultFocus } from './missionResultPresentation'

export const useMissionResultSelection = () => {
    const [params, setParams] = useSearchParams()
    const analysisId = params.get('analysisId')
    const selectedId = analysisId ?? params.get('inspectionId')
    const preferredFocus: ResultFocus = analysisId ? 'analysis' : 'inspection'

    const select = (id: string | undefined, focus: ResultFocus = preferredFocus) => {
        setParams(
            (previous) => {
                const next = new URLSearchParams(previous)
                next.delete('inspectionId')
                next.delete('analysisId')
                if (id) next.set(focus === 'analysis' ? 'analysisId' : 'inspectionId', id)
                return next
            },
            { replace: true }
        )
    }

    return { selectedId, preferredFocus, select }
}
