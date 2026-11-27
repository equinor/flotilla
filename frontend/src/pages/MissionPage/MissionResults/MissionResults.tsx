import { Typography } from '@equinor/eds-core-react'
import { useLanguageContext } from 'contexts/LanguageContext'
import { InspectionData, sortByTaskOrder, sortFindingsFirst } from 'models/InspectionRecord'
import { Task } from 'models/Task'
import { PendingResultPlaceholder } from 'pages/InspectionReportPage/InspectionReportImage'
import { ContentCard } from 'components/Styles/StyledComponents'
import styled from 'styled-components'
import { useRef } from 'react'
import { MissionResultGallery } from 'pages/MissionPage/MissionResults/MissionResultGallery'
import { MissionResultsSection } from 'pages/MissionPage/MissionResults/MissionResultsSection'
import { useMissionResultSelection } from './useMissionResultSelection'

const MissionContent = styled.div`
    min-width: 0;
`

interface Props {
    data: InspectionData[]
    tasks: Task[]
    isPending: boolean
    isError: boolean
    installationName: string
}

export type ResultFocus = 'inspection' | 'analysis'

export const MissionResults = ({ data, tasks, isPending, isError, installationName }: Props) => {
    const { TranslateText } = useLanguageContext()
    const { selectedId, preferredFocus, findingsFirst, select } = useMissionResultSelection()

    const sortedData = sortByTaskOrder(data, tasks)
    const findingsFirstData = sortFindingsFirst(sortedData)

    const container = useRef<HTMLDivElement>(null)
    const opener = useRef<HTMLElement | null>(null)

    const onSelect = (id: string, focus: ResultFocus) => {
        if (!selectedId && document.activeElement instanceof HTMLElement) opener.current = document.activeElement
        select(id, focus, true)
    }

    return (
        <MissionContent ref={container} tabIndex={-1}>
            {(isPending || isError || data.length > 0) && (
                <ContentCard>
                    {isPending && <PendingResultPlaceholder isLargeImage />}
                    {isError && (
                        <Typography role="alert">{TranslateText('Failed to load inspection results')}</Typography>
                    )}
                    {!isPending && <MissionResultsSection data={findingsFirstData} tasks={tasks} onSelect={onSelect} />}
                </ContentCard>
            )}
            {selectedId && !isPending && (
                <MissionResultGallery
                    data={findingsFirst ? findingsFirstData : sortedData}
                    tasks={tasks}
                    selectedId={selectedId}
                    preferredFocus={preferredFocus}
                    installationName={installationName}
                    loadFailed={isError}
                    onSelect={select}
                    onClose={() => select(undefined)}
                />
            )}
        </MissionContent>
    )
}
