import { Typography } from '@equinor/eds-core-react'
import { useLanguageContext } from 'contexts/LanguageContext'
import { hasInspectionAnalysis, InspectionData } from 'models/InspectionRecord'
import { Task } from 'models/Task'
import { PendingResultPlaceholder } from 'pages/InspectionReportPage/InspectionReportImage'
import { ContentCard } from 'components/Styles/StyledComponents'
import styled from 'styled-components'
import { useMemo, useRef } from 'react'
import { MissionResultGallery } from 'pages/MissionPage/MissionResults/MissionResultGallery'
import { MissionResultsSection } from 'pages/MissionPage/MissionResults/MissionResultsSection'
import { useSearchParams } from 'react-router'

const MissionContent = styled.div`
    min-width: 0;
`

interface Props {
    tasks: Pick<Task, 'id' | 'sensorType'>[]
    data: InspectionData[] | undefined
    isPending: boolean
    isError: boolean
    installationName: string
    robotName: string | undefined
}

export type ResultFocus = 'inspection' | 'analysis'
type MediaType = 'image' | 'video' | 'unsupported'
export type ResultMedia = { src: string; type: MediaType }

export interface MissionResult {
    inspection: InspectionData
    taskNumber: number
    sensorType: Task['sensorType']
    rawMedia?: ResultMedia
    analysisMedia?: ResultMedia
    measurementId?: string
    hasAnalysis: boolean
    hasFinding: boolean
}

const mediaTypeFromUrl = (src: string): MediaType => {
    try {
        const extension = new URL(src).pathname.split('.').pop()?.toLowerCase()
        if (['jpg', 'jpeg', 'png', 'gif'].includes(extension ?? '')) return 'image'
        else if (['mp4', 'mpg', 'mpeg', 'm4v'].includes(extension ?? '')) return 'video'
        else return 'unsupported'
    } catch {
        return 'unsupported'
    }
}

const getMissionResults = (
    tasks: Pick<Task, 'id' | 'sensorType'>[],
    inspections: InspectionData[]
): MissionResult[] => {
    const byId = new Map(inspections.map((inspection) => [inspection.inspectionId, inspection]))
    const results = tasks.flatMap((task, index) => {
        const inspection = byId.get(task.id)
        if (!inspection) return []
        const sourceUrl =
            inspection.anonymizedSAS ??
            (inspection.mediaSAS !== inspection.visualizedSAS ? inspection.mediaSAS : undefined)

        const measurementId = inspection.measurementRecordId
        const rawMediaType: MediaType | undefined =
            sourceUrl && !measurementId ? mediaTypeFromUrl(sourceUrl) : undefined
        const analysisMediaUrl = rawMediaType
            ? inspection.visualizedSAS !== sourceUrl
                ? inspection.visualizedSAS
                : undefined
            : undefined
        const analysisType: MediaType | undefined = analysisMediaUrl ? mediaTypeFromUrl(analysisMediaUrl) : undefined
        const hasAnalysis = hasInspectionAnalysis(inspection)
        return [
            {
                inspection,
                taskNumber: index + 1,
                sensorType: task.sensorType,
                rawMedia: rawMediaType ? { src: sourceUrl!, type: rawMediaType } : undefined,
                analysisMedia: analysisType ? { src: analysisMediaUrl!, type: analysisType } : undefined,
                measurementId,
                hasAnalysis,
                hasFinding: Boolean(inspection?.warning),
            },
        ]
    })
    return results.sort((left, right) => Number(right.hasFinding) - Number(left.hasFinding))
}

const useMissionResultSelection = () => {
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

    return (
        <MissionContent ref={container} tabIndex={-1}>
            {(isPending || isError || results.length > 0) && (
                <ContentCard>
                    {isPending && <PendingResultPlaceholder isLargeImage />}
                    {isError && (
                        <Typography role="alert">{TranslateText('Failed to load inspection results')}</Typography>
                    )}
                    {!isPending && <MissionResultsSection results={results} onSelect={onSelect} />}
                </ContentCard>
            )}
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
                />
            )}
        </MissionContent>
    )
}
