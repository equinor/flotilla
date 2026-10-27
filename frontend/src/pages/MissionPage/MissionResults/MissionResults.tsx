import { Typography } from '@equinor/eds-core-react'
import { useLanguageContext } from 'contexts/LanguageContext'
import { InspectionData } from 'models/InspectionRecord'
import { PendingResultPlaceholder } from 'pages/InspectionReportPage/InspectionReportImage'
import { ContentCard } from 'components/Styles/StyledComponents'
import styled from 'styled-components'
import { useRef } from 'react'
import { MissionResultGallery } from 'pages/MissionPage/MissionResults/MissionResultGallery'
import { MissionResultsSection } from 'pages/MissionPage/MissionResults/MissionResultsSection'
import { useSearchParams } from 'react-router'

const MissionContent = styled.div`
    min-width: 0;
`

interface Props {
    data: InspectionData[]
    isPending: boolean
    isError: boolean
    installationName: string
}

export type ResultFocus = 'inspection' | 'analysis'

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

export const MissionResults = ({ data, isPending, isError, installationName }: Props) => {
    const { TranslateText } = useLanguageContext()
    const { selectedId, preferredFocus, select } = useMissionResultSelection()

    const container = useRef<HTMLDivElement>(null)
    const opener = useRef<HTMLElement | null>(null)

    const onSelect = (id: string, focus: ResultFocus) => {
        if (!selectedId && document.activeElement instanceof HTMLElement) opener.current = document.activeElement
        select(id, focus)
    }

    return (
        <MissionContent ref={container} tabIndex={-1}>
            {(isPending || isError || data.length > 0) && (
                <ContentCard>
                    {isPending && <PendingResultPlaceholder isLargeImage />}
                    {isError && (
                        <Typography role="alert">{TranslateText('Failed to load inspection results')}</Typography>
                    )}
                    {!isPending && <MissionResultsSection data={data} onSelect={onSelect} />}
                </ContentCard>
            )}
            {selectedId && !isPending && (
                <MissionResultGallery
                    data={data}
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
