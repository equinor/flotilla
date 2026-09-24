import { Typography } from '@equinor/eds-core-react'
import { useLanguageContext } from 'contexts/LanguageContext'
import { InspectionData } from 'models/InspectionRecord'
import { Task } from 'models/Task'
import { PendingResultPlaceholder } from 'pages/InspectionReportPage/InspectionReportImage'
import { ReactNode, useMemo, useRef } from 'react'
import styled from 'styled-components'
import { getMissionResults } from './missionResultPresentation'
import { MissionResultGallery } from './MissionResultGallery'
import { useMissionResultSelection } from './useMissionResultSelection'

const MissionContent = styled.div`
    display: flex;
    flex-direction: column;
    min-width: 0;
    gap: 2rem;
`

interface Props {
    tasks: Pick<Task, 'id' | 'sensorType'>[]
    data: InspectionData[] | undefined
    isPending: boolean
    isError: boolean
    installationName: string
    robotName: string | undefined
    children: ReactNode
}

export const MissionResultGalleryController = ({
    tasks,
    data,
    isPending,
    isError,
    installationName,
    robotName,
    children,
}: Props) => {
    const { TranslateText } = useLanguageContext()
    const { selectedId, preferredFocus, select } = useMissionResultSelection()
    const results = useMemo(() => getMissionResults(tasks, data ?? []), [tasks, data])
    const container = useRef<HTMLDivElement>(null)
    const opener = useRef<HTMLElement | null>(null)

    const returnFocus = () => {
        const target = opener.current?.isConnected ? opener.current : container.current
        target?.focus({ preventScroll: true })
    }

    return (
        <MissionContent
            ref={container}
            tabIndex={-1}
            onFocusCapture={(event) => {
                if (!selectedId) opener.current = event.target
            }}
        >
            {children}
            {isPending && <PendingResultPlaceholder isLargeImage />}
            {isError && <Typography role="alert">{TranslateText('Failed to load inspection results')}</Typography>}
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
        </MissionContent>
    )
}
