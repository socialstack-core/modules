using Api.Database;
using System.Threading.Tasks;
using System.Collections.Generic;
using Api.Permissions;
using Api.Contexts;
using Api.Eventing;
using Amazon.S3;
using Amazon.S3.Transfer;
using Amazon.Runtime;
using Api.Uploader;
using System.IO;
using System;
using Api.Startup;
using System.Net.Http;

namespace Api.CloudHosts
{
	/// <summary>
	/// Instanced automatically. Use injection to use this service, or Startup.Services.Get.
	/// </summary>
    [LoadPriority(11)]
	public partial class CloudHostService : AutoService
    {
        private CloudHostConfig _config;

        /// <summary>
        /// Instanced automatically. Use injection to use this service, or Startup.Services.Get.
        /// </summary>
        public CloudHostService()
        {
            // Get config:
            _config = GetConfig<CloudHostConfig>();

            SelectConfiguredHost();

            _config.OnChange += () => {
                SelectConfiguredHost();

                return new ValueTask();
            };

            Events.Upload.StoreFile.AddEventListener(async (Context context, Upload upload, string tempFile, string variantName) => {

                if (upload != null)
                {
                    // If configured, the default file move is disabled and we're instead sending uploads to the configured host platform.

                    if (_uploadHost != null)
                    {
                        var result = await _uploadHost.Upload(context, upload, tempFile, variantName);
                        upload = null;

                        if (!result)
                        {
                            throw new PublicException("Unable to save your file at the moment", "internal_transfer_error");
                        }
                    }

                }

                return upload;
            }, 10);

            Events.Upload.ReadFile.AddEventListener(async (Context context, byte[] result, string relativePath, bool isPrivate) => {

                if (result != null)
                {
                    // Something else handled it.
                    return result;
                }
                    
                // If configured, the default file move is disabled and we're instead reading uploads from the configured host platform.
                if (_uploadHost != null)
                {

                    if (isPrivate)
                    {
                        var stream = await _uploadHost.ReadFile(relativePath, isPrivate);
                        var ms = new MemoryStream();
                        await stream.CopyToAsync(ms);
                        return ms.ToArray();
                    }
                    else
                    {
                        // Public URL:
                        var url = _uploadHost.GetContentUrl() + "/content/" + relativePath;

                        // Request the file:
                        var client = new HttpClient();

                        try
                        {
                            // Get the bytes:
                            result = await client.GetByteArrayAsync(url);
                        }
                        catch(Exception e)
                        {
                            try
                            {
                                var stream = await _uploadHost.ReadFile(relativePath, isPrivate);
                                var ms = new MemoryStream();
                                await stream.CopyToAsync(ms);
                                return ms.ToArray();
                            }
                            catch
                            {
                                Console.WriteLine("Likely temporary error whilst trying to read a file from a remote host: " + e.ToString());
                                // Unavailable or unreachable.
                                return null;
                            }
                        }
                    }
                }

                return result;
            }, 10);

        }

        private List<CloudHostPlatform> _platforms;

        /// <summary>
        /// The host to direct uploads to, if any.
        /// </summary>
        private CloudHostPlatform _uploadHost;

        /// <summary>
        /// Updates the preferred host based on what is actually configured.
        /// </summary>
        private void SelectConfiguredHost()
        {
            // Depending on what is configured will affect what services are used for what things.

            var newPlatformSet = new List<CloudHostPlatform>();

            if (_config.DigitalOcean != null)
            {
                newPlatformSet.Add(new DigitalOceanHost(this, _config.DigitalOcean));
            }

            if (_config.AWS != null)
            {
                newPlatformSet.Add(new AwsHost(this, _config.AWS));
            }

            if (_config.Azure != null)
            {
                newPlatformSet.Add(new AzureHost(this, _config.Azure));
            }

            _platforms = newPlatformSet;

            CloudHostPlatform uploadHost = null;

            // Find a host with an upload service:
            foreach (var host in newPlatformSet)
            {
                if (host.HasService("upload"))
                {
                    uploadHost = host;
                    break;
                }
            }

            _uploadHost = uploadHost;

            var frontend = Services.Get("FrontendCodeService");

            if (frontend != null)
            {
                var setContentUrl = frontend.GetType().GetMethod("SetContentUrl");

                if (_uploadHost != null)
                {
                    setContentUrl.Invoke(frontend, new object[] { _uploadHost.GetContentUrl() });
                }
                else
                {
                    setContentUrl.Invoke(frontend, new object[] { null });
                }
            }
        }
    }

}

/*
{
  "Settings": {
    "Inputs": [
      {
        "TimecodeSource": "ZEROBASED",
        "VideoSelector": {},
        "AudioSelectors": {
          "Audio Selector 1": {
            "DefaultSelection": "DEFAULT"
          }
        },
        "FileInput": "https://shf-sbm.ams3.digitaloceanspaces.com/content/1-original.mp4"
      }
    ],
    "OutputGroups": [
      {
        "Name": "DASH ISO",
        "OutputGroupSettings": {
          "Type": "DASH_ISO_GROUP_SETTINGS",
          "DashIsoGroupSettings": {
            "SegmentLength": 30,
            "FragmentLength": 2,
            "Destination": "s3://test"
          }
        },
        "Outputs": [
          {
            "VideoDescription": {
              "CodecSettings": {
                "Codec": "H_264",
                "H264Settings": {
                  "RateControlMode": "QVBR",
                  "SceneChangeDetect": "TRANSITION_DETECTION",
                  "MaxBitrate": 5300000
                }
              },
              "Width": 1920,
              "Height": 1080
            },
            "AudioDescriptions": [
              {
                "CodecSettings": {
                  "Codec": "AAC",
                  "AacSettings": {
                    "Bitrate": 96000,
                    "CodingMode": "CODING_MODE_2_0",
                    "SampleRate": 48000
                  }
                }
              }
            ],
            "ContainerSettings": {
              "Container": "MPD"
            },
            "NameModifier": "1080p"
          },
          {
            "VideoDescription": {
              "CodecSettings": {
                "Codec": "H_264",
                "H264Settings": {
                  "RateControlMode": "QVBR",
                  "SceneChangeDetect": "TRANSITION_DETECTION",
                  "MaxBitrate": 3000000
                }
              },
              "Width": 1280,
              "Height": 720
            },
            "AudioDescriptions": [
              {
                "CodecSettings": {
                  "Codec": "AAC",
                  "AacSettings": {
                    "Bitrate": 96000,
                    "CodingMode": "CODING_MODE_2_0",
                    "SampleRate": 48000
                  }
                }
              }
            ],
            "ContainerSettings": {
              "Container": "MPD"
            },
            "NameModifier": "720p"
          },
          {
            "VideoDescription": {
              "CodecSettings": {
                "Codec": "H_264",
                "H264Settings": {
                  "RateControlMode": "QVBR",
                  "SceneChangeDetect": "TRANSITION_DETECTION",
                  "MaxBitrate": 1000000
                }
              },
              "Width": 854,
              "Height": 480
            },
            "AudioDescriptions": [
              {
                "CodecSettings": {
                  "Codec": "AAC",
                  "AacSettings": {
                    "Bitrate": 96000,
                    "CodingMode": "CODING_MODE_2_0",
                    "SampleRate": 48000
                  }
                }
              }
            ],
            "ContainerSettings": {
              "Container": "MPD"
            },
            "NameModifier": "480p"
          },
          {
            "VideoDescription": {
              "CodecSettings": {
                "Codec": "H_264",
                "H264Settings": {
                  "RateControlMode": "QVBR",
                  "SceneChangeDetect": "TRANSITION_DETECTION",
                  "MaxBitrate": 750000
                }
              },
              "Width": 640,
              "Height": 360
            },
            "AudioDescriptions": [
              {
                "CodecSettings": {
                  "Codec": "AAC",
                  "AacSettings": {
                    "Bitrate": 96000,
                    "CodingMode": "CODING_MODE_2_0",
                    "SampleRate": 48000
                  }
                }
              }
            ],
            "ContainerSettings": {
              "Container": "MPD"
            },
            "NameModifier": "360p"
          },
          {
            "VideoDescription": {
              "CodecSettings": {
                "Codec": "H_264",
                "H264Settings": {
                  "RateControlMode": "QVBR",
                  "SceneChangeDetect": "TRANSITION_DETECTION",
                  "MaxBitrate": 400000
                }
              },
              "Width": 426,
              "Height": 240
            },
            "AudioDescriptions": [
              {
                "CodecSettings": {
                  "Codec": "AAC",
                  "AacSettings": {
                    "Bitrate": 96000,
                    "CodingMode": "CODING_MODE_2_0",
                    "SampleRate": 48000
                  }
                }
              }
            ],
            "ContainerSettings": {
              "Container": "MPD"
            },
            "NameModifier": "240p"
          },
          {
            "VideoDescription": {
              "CodecSettings": {
                "Codec": "H_264",
                "H264Settings": {
                  "RateControlMode": "QVBR",
                  "SceneChangeDetect": "TRANSITION_DETECTION",
                  "MaxBitrate": 235000
                }
              },
              "Width": 256,
              "Height": 144
            },
            "AudioDescriptions": [
              {
                "CodecSettings": {
                  "Codec": "AAC",
                  "AacSettings": {
                    "Bitrate": 96000,
                    "CodingMode": "CODING_MODE_2_0",
                    "SampleRate": 48000
                  }
                }
              }
            ],
            "ContainerSettings": {
              "Container": "MPD"
            },
            "NameModifier": "144p"
          }
        ]
      }
    ],
    "TimecodeConfig": {
      "Source": "ZEROBASED"
    }
  }
}
*/