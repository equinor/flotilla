import { Typography } from '@equinor/eds-core-react'
import { useLanguageContext } from 'contexts/LanguageContext'
import { InspectionData } from 'models/InspectionRecord'
import { Task } from 'models/Task'
import { PendingResultPlaceholder } from 'pages/InspectionReportPage/InspectionReportImage'
import { useMemo, useRef } from 'react'
import { getMissionResults, ResultFocus } from './missionResultPresentation'
import { MissionResultGallery } from './MissionResultGallery'
import { MissionResultsSection } from './MissionResultsSection'
import { useMissionResultSelection } from './useMissionResultSelection'

interface Props {
    tasks: Pick<Task, 'id' | 'sensorType'>[]
    data: InspectionData[] | undefined
    isPending: boolean
    isError: boolean
    installationName: string
    robotName: string | undefined
}

export const MissionResults = ({ tasks, data, isPending, isError, installationName, robotName }: Props) => {
    const { TranslateText } = useLanguageContext()
    const { selectedId, preferredFocus, select } = useMissionResultSelection()
    const results = useMemo(() => getMissionResults(tasks, data ?? []), [tasks, data])
    const container = useRef<HTMLDivElement>(null)
    const opener = useRef<HTMLElement | null>(null)

    const onSelect = (id: string, focus: ResultFocus) => {
        if (!selectedId && document.activeElement instanceof HTMLElement) opener.current = document.activeElement
        select(id, focus)
    }
    const returnFocus = () => {
        const target = opener.current?.isConnected ? opener.current : container.current
        target?.focus({ preventScroll: true })
    }

    return (
        <div ref={container} tabIndex={-1}>
            {isPending && <PendingResultPlaceholder isLargeImage />}
            {isError && <Typography role="alert">{TranslateText('Failed to load inspection results')}</Typography>}
            {!isPending && <MissionResultsSection results={results} onSelect={onSelect} />}
            {selectedId && !isPending && (
                <MissionResultGallery
                    results={results}
                    selectedId={selectedId}
                    preferredFocus={preferredFocus}
                    installationName={installationName}
                    robotName={robotName}
                    loadFailed={isError}
                    onSelect={select}
                    onClose={() => select(undefined)}
                    returnFocus={returnFocus}
                />
            )}
        </div>
    )
}
