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
import { ResultFocus } from './MissionResults'
import { hasInspectionAnalysis, InspectionData } from 'models/InspectionRecord'

interface Props {
    data: InspectionData[]
    selectedId: string
    preferredFocus: ResultFocus
    installationName: string
    loadFailed: boolean
    onSelect: (id: string, focus?: ResultFocus) => void
    onClose: () => void
}

export const MissionResultGallery = ({
    data,
    selectedId,
    preferredFocus,
    installationName,
    loadFailed,
    onSelect,
    onClose,
}: Props) => {
    const { TranslateText } = useLanguageContext()
    const index = data.findIndex((inspection) => inspection.inspectionId === selectedId)
    const inspection = data[index]
    const focus =
        inspection && preferredFocus === 'analysis' && hasInspectionAnalysis(inspection) ? 'analysis' : 'inspection'

    const navigate = (offset: number) => {
        const next = data[index + offset]
        if (index !== -1 && next) onSelect(next.inspectionId)
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
                        {inspection && (
                            <GalleryTaskNumber variant="caption">
                                {TranslateText('Result task {0}', [String(index)])}
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
                            {index + 1} / {data.length}
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
                        disabled={index === -1 || index === data.length - 1}
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
            {inspection ? (
                <GalleryResultContent
                    inspection={inspection}
                    focus={focus}
                    installationName={installationName}
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
                    {data.map((inspection, inspectionIndex) => {
                        const id = inspection.inspectionId
                        const thumbnailSource = inspection.anonymizedSAS ?? inspection.visualizedSAS
                        return (
                            <StripButton
                                key={id}
                                $hasFinding={
                                    hasInspectionAnalysis(inspection) ? Boolean(inspection?.warning) : undefined
                                }
                                variant="ghost"
                                aria-current={id === selectedId ? 'true' : undefined}
                                aria-label={TranslateText('View result for {0}, task {1}', [
                                    inspection.tag,
                                    String(inspectionIndex),
                                ])}
                                onClick={() => onSelect(id)}
                            >
                                {!inspection.measurementRecordId && thumbnailSource && (
                                    <ResultMediaView
                                        source={thumbnailSource}
                                        label={inspection.tag}
                                        fileType={inspection.fileType}
                                        size="thumbnail"
                                    />
                                )}
                                {inspection.measurementRecordId && (
                                    <MeasurementDisplay recordId={inspection.measurementRecordId!} isCompact={true} />
                                )}
                                <Typography variant="body_short">{inspection.tag}</Typography>
                                <Typography variant="caption">
                                    {TranslateText('Result task {0}', [String(inspectionIndex)])}
                                </Typography>
                            </StripButton>
                        )
                    })}
                </div>
            </Strip>
        </Gallery>
    )
}
