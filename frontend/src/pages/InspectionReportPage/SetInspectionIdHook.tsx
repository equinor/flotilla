import { useSearchParams } from 'react-router-dom'

export const useInspectionId = () => {
    // eslint-disable-next-line @typescript-eslint/no-unused-vars
    const [searchParams, setSearchParams] = useSearchParams()

    const switchSelectedInspectionId = (inspectionId: string | undefined) => {
        setSearchParams((prev) => {
            if (inspectionId) prev.set('inspectionId', inspectionId)
            else prev.delete('inspectionId')
            return prev
        })
    }

    return { switchSelectedInspectionId }
}
