import { useSearchParams } from 'react-router'
import { ResultFocus } from './MissionResults'

export const useMissionResultSelection = () => {
    const [params, setParams] = useSearchParams()
    const selectedId = params.get('inspectionId')
    const focusString = params.get('focus')
    const preferredFocus: ResultFocus =
        focusString && (focusString === 'analysis' || focusString === 'inspection') ? focusString : 'analysis'
    const findingsFirst = params.get('order') === 'findings'

    const select = (
        id: string | undefined,
        focus: ResultFocus = preferredFocus,
        selectFindingsFirst: boolean = findingsFirst
    ) => {
        setParams(
            (previous) => {
                const next = new URLSearchParams(previous)
                next.delete('inspectionId')
                next.delete('focus')
                next.delete('order')
                if (id) {
                    next.set('inspectionId', id)
                    next.set('focus', focus)
                    if (selectFindingsFirst) next.set('order', 'findings')
                }
                return next
            },
            { replace: true }
        )
    }

    return { selectedId, preferredFocus, findingsFirst, select }
}
