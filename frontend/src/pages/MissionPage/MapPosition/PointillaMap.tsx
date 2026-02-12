import { useEffect, useRef } from 'react'
import { useMap } from 'react-leaflet'
import { DoneCallback, TileLayerOptions, TileLayer, Coords } from 'leaflet'
import 'leaflet/dist/leaflet.css'
import { PointillaMapInfo } from 'models/PointillaMapInfo'
import { useBackendApi } from 'api/UseBackendApi'
/* eslint-disable react/prop-types */

interface TileLayerHeadersOptions extends TileLayerOptions {
    customHeaders?: Record<string, string>
    getBlob?: (url: string, opts?: { headers?: Record<string, string>; signal?: AbortSignal }) => Promise<Blob>
}
class TileLayerHeaders extends TileLayer {
    declare options: TileLayerHeadersOptions

    constructor(urlTemplate: string, options: TileLayerHeadersOptions) {
        super(urlTemplate, options)
    }

    createTile(coords: Coords, done: DoneCallback): HTMLImageElement {
        const img = document.createElement('img')

        if (this.options.crossOrigin || this.options.crossOrigin === '') {
            img.crossOrigin = this.options.crossOrigin === true ? '' : this.options.crossOrigin
        }
        if (typeof this.options.referrerPolicy === 'string') {
            img.referrerPolicy = this.options.referrerPolicy
        }
        img.alt = ''

        const controller = new AbortController()
        const url = this.getTileUrl(coords)

        const getBlob =
            this.options.getBlob ??
            ((u: string, opts?: { headers?: Record<string, string>; signal?: AbortSignal }) =>
                fetch(u, {
                    headers: this.options.customHeaders,
                    signal: opts?.signal,
                }).then((r) => {
                    if (!r.ok) throw new Error(`HTTP ${r.status} loading ${u}`)
                    return r.blob()
                }))

        let objectUrl: string | null = null

        const cleanup = () => {
            if (objectUrl) {
                URL.revokeObjectURL(objectUrl)
                objectUrl = null
            }
        }

        img.onload = () => {
            cleanup()
            done(undefined, img)
        }
        img.onerror = () => {
            const err = new Error('Image failed to load')
            cleanup()
            done(err as any, img)
        }

        getBlob(url, { headers: this.options.customHeaders, signal: controller.signal })
            .then((blob) => {
                objectUrl = URL.createObjectURL(blob)
                img.src = objectUrl!
            })
            .catch((err) => {
                cleanup()
                done(err as any, img)
            })
        ;(img as any)._abortController = controller
        return img
    }
}

interface AuthTileLayerProps {
    mapInfo: PointillaMapInfo
}

const AuthTileLayer: React.FC<AuthTileLayerProps> = ({ mapInfo }) => {
    const map = useMap()
    const tileLayerRef = useRef<TileLayerHeaders | null>(null)
    const backendApi = useBackendApi()

    useEffect(() => {
        if (tileLayerRef.current) {
            map.removeLayer(tileLayerRef.current)
            tileLayerRef.current = null
        }
        if (mapInfo === undefined) return
        const bounds: [[number, number], [number, number]] = [
            [mapInfo.yMin + 1, mapInfo.xMin + 1], // Southwest corner
            [mapInfo.yMax, mapInfo.xMax], // Northeast corner
        ]

        const tileLayer = new TileLayerHeaders(
            `pointilla/map/tiles/${mapInfo.plantCode.toUpperCase()}/${mapInfo.floorId}/{z}/{x}/{y}`,
            {
                bounds: bounds,
                minZoom: mapInfo.zoomMin,
                maxZoom: mapInfo.zoomMax,
                tileSize: mapInfo.tileSize,
                getBlob: getBlobViaBackend(backendApi),
            }
        )

        tileLayer.addTo(map)
        tileLayerRef.current = tileLayer

        return () => {
            if (tileLayerRef.current) {
                map.removeLayer(tileLayerRef.current)
                tileLayerRef.current = null
            }
        }
    }, [map, mapInfo])
    return null
}

function getBlobViaBackend(backendApi: ReturnType<typeof useBackendApi>) {
    return (url: string, opts?: { headers?: Record<string, string>; signal?: AbortSignal }): Promise<Blob> => {
        const path = url.replace(/^https?:\/\/[^/]+\//, '')
        return backendApi.getFloorMapTileByPath(path, {
            signal: opts?.signal,
            headers: opts?.headers,
        })
    }
}

export default AuthTileLayer
