import { Button, Icon, Typography } from '@equinor/eds-core-react'
import { chevron_left, chevron_right, close } from '@equinor/eds-icons'
import { useLanguageContext } from 'contexts/LanguageContext'
import { KeyboardEvent } from 'react'
import { ResultMediaView } from './ResultMediaView'
import { GalleryResultContent } from './GalleryResultContent'
import {
    Gallery,
    GalleryBody,
    GalleryDescription,
    GalleryDetails,
    GalleryHeader,
    GalleryHeading,
    GalleryTaskNumber,
    Navigation,
    Strip,
    StripButton,
} from './MissionResultStyles'
import { MeasurementDisplay } from 'components/Displays/MeasurementDisplay'
import { MissionResult, ResultFocus } from './MissionResults'

interface Props {
    results: MissionResult[]
    selectedId: string
    preferredFocus: ResultFocus
    installationName: string
    robotName: string | undefined
    loadFailed: boolean
    onSelect: (id: string, focus?: ResultFocus) => void
    onClose: () => void
}

export const MissionResultGallery = ({
    results,
    selectedId,
    preferredFocus,
    installationName,
    robotName,
    loadFailed,
    onSelect,
    onClose,
}: Props) => {
    const { TranslateText } = useLanguageContext()
    const index = results.findIndex((result) => result.inspection.inspectionId === selectedId)
    const result = results[index]
    const focus = result && preferredFocus === 'analysis' && result.hasAnalysis ? 'analysis' : 'inspection'
    const inspection = result?.inspection

    const navigate = (offset: number) => {
        const next = results[index + offset]
        if (index !== -1 && next) onSelect(next.inspection.inspectionId)
    }

    const onKeyDown = (event: KeyboardEvent<HTMLElement>) => {
        if (event.key === 'ArrowLeft' || event.key === 'ArrowRight') {
            event.preventDefault()
            navigate(event.key === 'ArrowLeft' ? -1 : 1)
        }
    }

    return (
        <Gallery open isDismissable onClose={onClose} onKeyDown={onKeyDown}>
            <GalleryHeader>
                <GalleryDetails>
                    <GalleryHeading>
                        <Typography variant="h3" tabIndex={-1}>
                            {inspection?.tag || TranslateText('Inspection results')}
                        </Typography>
                        {result && (
                            <GalleryTaskNumber variant="caption">
                                {TranslateText('Result task {0}', [String(result.taskNumber)])}
                            </GalleryTaskNumber>
                        )}
                    </GalleryHeading>
                    {inspection?.inspectionDescription && (
                        <GalleryDescription variant="body_long">{inspection.inspectionDescription}</GalleryDescription>
                    )}
                </GalleryDetails>
                <Navigation>
                    {index !== -1 && (
                        <Typography variant="caption">
                            {index + 1} / {results.length}
                        </Typography>
                    )}
                    <Button
                        variant="ghost_icon"
                        aria-label={TranslateText('Previous inspection')}
                        disabled={index <= 0}
                        onClick={() => navigate(-1)}
                    >
                        <Icon data={chevron_left} />
                    </Button>
                    <Button
                        variant="ghost_icon"
                        aria-label={TranslateText('Next inspection')}
                        disabled={index === -1 || index === results.length - 1}
                        onClick={() => navigate(1)}
                    >
                        <Icon data={chevron_right} />
                    </Button>
                    <Button variant="ghost_icon" aria-label={TranslateText('Close result gallery')} onClick={onClose}>
                        <Icon data={close} />
                    </Button>
                </Navigation>
            </GalleryHeader>
            {loadFailed && (
                <GalleryBody $paired={false}>
                    <Typography role="alert">{TranslateText('Failed to load inspection results')}</Typography>
                </GalleryBody>
            )}
            {result ? (
                <GalleryResultContent
                    result={result}
                    focus={focus}
                    installationName={installationName}
                    robotName={robotName}
                    onFocus={(newFocus) => {
                        onSelect(selectedId, newFocus)
                    }}
                />
            ) : !loadFailed ? (
                <GalleryBody $paired={false}>
                    <Typography role="alert">{TranslateText('Selected result is unavailable')}</Typography>
                </GalleryBody>
            ) : null}
            <Strip aria-label={TranslateText('Inspections in this mission')}>
                <Typography variant="h6">{TranslateText('Inspections in this mission')}</Typography>
                <div>
                    {results.map((item) => {
                        const id = item.inspection.inspectionId
                        const thumbnail = item.rawMedia ?? item.analysisMedia
                        return (
                            <StripButton
                                key={id}
                                $hasFinding={item.hasAnalysis ? item.hasFinding : undefined}
                                variant="ghost"
                                aria-current={id === selectedId ? 'true' : undefined}
                                aria-label={TranslateText('View result for {0}, task {1}', [
                                    item.inspection.tag,
                                    String(item.taskNumber),
                                ])}
                                onClick={() => onSelect(id)}
                            >
                                {!item.measurementId && thumbnail && (
                                    <ResultMediaView media={thumbnail} label={item.inspection.tag} size="thumbnail" />
                                )}
                                {item.measurementId && (
                                    <MeasurementDisplay recordId={result.measurementId!} isCompact={true} />
                                )}
                                <Typography variant="body_short">{item.inspection.tag}</Typography>
                                <Typography variant="caption">
                                    {TranslateText('Result task {0}', [String(item.taskNumber)])}
                                </Typography>
                            </StripButton>
                        )
                    })}
                </div>
            </Strip>
        </Gallery>
    )
}
