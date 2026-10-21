import { useSearchParams } from 'react-router'
import { ResultFocus } from './missionResultPresentation'

export const useMissionResultSelection = () => {
    const [params, setParams] = useSearchParams()
    const selectedId = params.get('inspectionId')
    const focusString = params.get('focus')
    const preferredFocus: ResultFocus =
        focusString && (focusString === 'analysis' || focusString === 'inspection') ? focusString : 'analysis'

    const select = (id: string | undefined, focus: ResultFocus = preferredFocus) => {
        setParams(
            (previous) => {
                const next = new URLSearchParams(previous)
                next.delete('inspectionId')
                next.delete('focus')
                if (id) {
                    next.set('inspectionId', id)
                    next.set('focus', focus)
                }
                return next
            },
            { replace: true }
        )
    }

    return { selectedId, preferredFocus, select }
}
